import os
import uvicorn
import re
import xml.etree.ElementTree as ET
from contextlib import asynccontextmanager
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import pdfplumber  
import pandas as pd
from langchain_core.documents import Document
from langchain_text_splitters import RecursiveCharacterTextSplitter
from langchain_chroma import Chroma
from langchain_groq import ChatGroq
from langchain_huggingface import HuggingFaceEmbeddings
from langchain_classic.chains import create_retrieval_chain  
from langchain_classic.chains.combine_documents import create_stuff_documents_chain  
from langchain_core.prompts import ChatPromptTemplate
from dotenv import load_dotenv

# ==========================================
# 🔑 CONFIGURACIÓN SEGURA DE ENTORNO
# ==========================================
load_dotenv()
GROQ_API_KEY = os.getenv("GROQ_API_KEY")

if not GROQ_API_KEY:
    os.environ["GROQ_API_KEY"] = "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"
else:
    os.environ["GROQ_API_KEY"] = GROQ_API_KEY

CARPETA_PDFS = r"C:\Users\Usuario\Desktop\automatizar\mi IA\mis_pdfs" 
CARPETA_VECTORIAL = "./base_datos_chroma_multi"

sistema_ia = None
ultima_respuesta_ia = "" 

class Consulta(BaseModel):
    pregunta: str

def extraer_texto_de_xml(ruta_xml):
    """Extrae todo el texto visible de un archivo XML/XLM de forma segura."""
    try:
        tree = ET.parse(ruta_xml)
        root = tree.getroot()
        texto_completo = "".join(root.itertext())
        return texto_completo.strip()
    except Exception as e:
        print(f"❌ Error al leer el archivo XML {ruta_xml}: {e}")
        return ""

def cargar_multiples_documentos(ruta_carpeta):
    """Escanea la carpeta leyendo tanto archivos PDF como XML/XLM."""
    if not os.path.exists(ruta_carpeta):
        os.makedirs(ruta_carpeta)
        return []
    
    documentos_langchain = []
    archivos = os.listdir(ruta_carpeta)
    
    for nombre_archivo in archivos:
        ruta_completa = os.path.join(ruta_carpeta, nombre_archivo)
        ext = nombre_archivo.lower()
        
        if ext.endswith('.pdf'):
            try:
                with pdfplumber.open(ruta_completa) as pdf:
                    for numero_pagina, pagina in enumerate(pdf.pages):
                        texto = pagina.extract_text()
                        if texto and texto.strip():
                            doc = Document(
                                page_content=texto,
                                metadata={"source": str(nombre_archivo), "page": int(numero_pagina + 1)}
                            )
                            documentos_langchain.append(doc)
            except Exception:
                continue
                
        elif ext.endswith('.xml') or ext.endswith('.xlm'):
            texto_xml = extraer_texto_de_xml(ruta_completa)
            if texto_xml:
                doc = Document(
                    page_content=texto_xml,
                    metadata={"source": str(nombre_archivo), "page": 1}
                )
                documentos_langchain.append(doc)
                
    return documentos_langchain

def inicializar_sistema():
    """Genera los vectores en el disco y conecta la base de datos con Groq."""
    global sistema_ia
    print("🧠 Inicializando modelo de lenguaje para procesamiento de texto...")
    embeddings = HuggingFaceEmbeddings(model_name="sentence-transformers/all-MiniLM-L6-v2")
    
    if os.path.exists(CARPETA_VECTORIAL):
        print("📚 Reutilizando base de datos híbrida indexada en el disco...")
        base_datos = Chroma(persist_directory=CARPETA_VECTORIAL, embedding_function=embeddings)
    else:
        print("⏳ Procesando tus archivos PDF y XML por primera vez...")
        documentos = cargar_multiples_documentos(CARPETA_PDFS)
        if not documentos:
            base_datos = Chroma.from_documents(
                documents=[Document(page_content="Carpeta vacía", metadata={"source": "vacio"})], 
                embedding=embeddings, 
                persist_directory=CARPETA_VECTORIAL
            )
        else:
            separador = RecursiveCharacterTextSplitter(chunk_size=1000, chunk_overlap=200)
            fragmentos = separador.split_documents(documentos)
            base_datos = Chroma.from_documents(
                documents=fragmentos, 
                embedding=embeddings, 
                persist_directory=CARPETA_VECTORIAL
            )
        print("✅ ¡Documentos indexados con éxito!")
    
    recuperador = base_datos.as_retriever(search_kwargs={"k": 5})
    llm = ChatGroq(model="llama-3.1-8b-instant", temperature=0.1)
    
    instrucciones = (
        "Sos un asistente de IA experto en análisis técnico de documentos (PDFs y XMLs).\n"
        "Tu único objetivo es responder la pregunta del usuario utilizando de forma estricta "
        "y exclusiva el contexto provisto abajo. No uses conocimientos externos.\n\n"
        
        "REGLAS OBLIGATORIAS DE RESPUESTA (REQUERIMIENTO DE INTERFAZ):\n"
        "1. IDIOMA Y TONO: Responde siempre en español de Argentina de forma clara y directa.\n"
        "2. ESTRUCTURA DE TABLAS (CRUCIAL): Si los datos del contexto contienen matrices, cuadros técnicos o dosificaciones, DEBES armar obligatoriamente tablas usando el formato Markdown clásico con barras verticales.\n"
        "Ejemplo exacto de formato obligatorio:\n"
        "| Clase de Herbicida | Efecto | Uso |\n"
        "| --- | --- | --- |\n"
        "| Selectivos | Matan solo malezas | Cultivos |\n"
        "Usa el formato tabular siempre que sea lógicamente posible ordenar la información en filas y columnas.\n"
        "3. TRATAMIENTO DE LA DUDA: Si la respuesta no figura explícitamente en el contexto, di exactamente: "
        "'Lo siento, no encuentro esa información en los documentos.' No inventes nada bajo ninguna circunstancia.\n\n"
        "Contexto:\n{context}"
    )
    prompt = ChatPromptTemplate.from_messages([("system", instrucciones), ("human", "{input}")])
    cadena_documentos = create_stuff_documents_chain(llm, prompt)
    sistema_ia = create_retrieval_chain(recuperador, cadena_documentos)
    print("🚀 Servidor de IA indexado y listo.")

@asynccontextmanager
async def lifespan(app: FastAPI):
    inicializar_sistema()
    yield

app = FastAPI(title="API Servidor RAG", lifespan=lifespan)

@app.get("/")
def ruta_raiz():
    return {"status": "online"}

@app.post("/preguntar")
def preguntar_ia(consulta: Consulta):
    global sistema_ia, ultima_respuesta_ia
    if not sistema_ia:
        raise HTTPException(status_code=503, detail="El sistema de IA no está inicializado.")
    try:
        resultado = sistema_ia.invoke({"input": consulta.pregunta})
        ultima_respuesta_ia = resultado["answer"]
        
        fuentes = []
        documentos_recuperados = resultado.get("context", resultado.get("source_documents", []))
            
        for doc in documentos_recuperados:
            archivo = doc.metadata.get("source", "Desconocido")
            pagina = doc.metadata.get("page", 1)
            
            if archivo and archivo != "vacio" and archivo != "Desconocido":
                if archivo.lower().endswith(('.xml', '.xlm')):
                    fuentes.append(f"• {archivo}")
                else:
                    fuentes.append(f"• {archivo} (Pág. {pagina})")
                    
        fuentes_unicas = list(sorted(set(fuentes)))
        return {"respuesta": resultado["answer"], "fuentes": fuentes_unicas}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/exportar")
def exportar_a_excel():
    global ultima_respuesta_ia
    if not ultima_respuesta_ia:
        raise HTTPException(status_code=400, detail="No hay datos previos para exportar.")
    try:
        lineas = ultima_respuesta_ia.split('\n')
        tablas_encontradas = []
        tabla_actual = []
        
        for linea in lineas:
            if '|' in linea:
                if re.match(r'^\s*\|[\s\-:|]*\|\s*$', linea):
                    continue
                celdas = [c.strip() for c in linea.split('|')[1:-1]]
                if celdas:
                    tabla_actual.append(celdas)
            else:
                if tabla_actual:
                    tablas_encontradas.append(tabla_actual)
                    tabla_actual = []
        if tabla_actual:
            tablas_encontradas.append(tabla_actual)
            
        if not tablas_encontradas:
            raise HTTPException(status_code=404, detail="No se encontraron tablas.")
            
        datos_tabla = tablas_encontradas[0]
        if len(datos_tabla) < 2:
            raise HTTPException(status_code=404, detail="La tabla no contiene datos suficientes.")
            
        encabezados = datos_tabla[0]
        filas = datos_tabla[1:]
        
        num_columnas = len(encabezados)
        filas_normalizadas = []
        for f in filas:
            if len(f) < num_columnas:
                f = f + [""] * (num_columnas - len(f))
            elif len(f) > num_columnas:
                f = f[:num_columnas]
            filas_normalizadas.append(f)
        
        df = pd.DataFrame(filas_normalizadas, columns=encabezados)
        ruta_escritorio = os.path.join(os.environ['USERPROFILE'], 'Desktop')
        ruta_excel = os.path.join(ruta_escritorio, 'Tabla_Exportada_IA.xlsx')
        
        # Guardamos usando openpyxl de forma avanzada para aplicar estilos
        with pd.ExcelWriter(ruta_excel, engine='openpyxl') as writer:
            df.to_excel(writer, index=False, sheet_name="Datos IA")
            
            # Importamos las herramientas de diseño de openpyxl
            from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
            
            workbook = writer.book
            worksheet = writer.sheets["Datos IA"]
            
            # Estilos para la cabecera (Azul oscuro, letra blanca, negrita, centrado)
            fill_cabecera = PatternFill(start_color="1F4E78", end_color="1F4E78", fill_type="solid")
            font_cabecera = Font(name="Calibri", size=11, bold=True, color="FFFFFF")
            align_centro = Alignment(horizontal="center", vertical="center", wrap_text=True)
            
            # Bordes finos grises para toda la tabla
            borde_fino = Side(border_style="thin", color="D9D9D9")
            cuadrícula = Border(left=borde_fino, right=borde_fino, top=borde_fino, bottom=borde_fino)
            
            # Aplicamos diseño a la fila de títulos
            for cell in worksheet[1]:
                cell.fill = fill_cabecera
                cell.font = font_cabecera
                cell.alignment = align_centro
                cell.border = cuadrícula
            
            # Autoajuste automático de columnas según el largo del texto + margen
            for col in worksheet.columns:
                max_len = 0
                col_letter = col[0].column_letter
                for cell in col:
                    if cell.row > 1: # Formateamos celdas de datos
                        cell.border = cuadrícula
                        cell.alignment = Alignment(vertical="center")
                    if cell.value:
                        max_len = max(max_len, len(str(cell.value)))
                # Le damos un aire de 4 caracteres extra para que no quede pegado
                worksheet.column_dimensions[col_letter].width = max(max_len + 4, 12)
                
        return {"status": "success", "archivo": ruta_excel}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8000)
