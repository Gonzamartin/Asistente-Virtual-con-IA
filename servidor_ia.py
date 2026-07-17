import os
import uvicorn
import xml.etree.ElementTree as ET
from contextlib import asynccontextmanager
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import pdfplumber  
from langchain_core.documents import Document
from langchain_text_splitters import RecursiveCharacterTextSplitter
from langchain_chroma import Chroma
from langchain_groq import ChatGroq
from langchain_huggingface import HuggingFaceEmbeddings
from langchain_classic.chains import create_retrieval_chain  
from langchain_classic.chains.combine_documents import create_stuff_documents_chain  
from langchain_core.prompts import ChatPromptTemplate

# ==========================================
# 🔑 CONFIGURACIÓN DE APIS Y CARPETAS
# ==========================================
os.environ["GROQ_API_KEY"] = "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"
CARPETA_PDFS = r"C:\Users\Usuario\Desktop\automatizar\mi IA\mis_pdfs" 
CARPETA_VECTORIAL = "./base_datos_chroma_multi"

sistema_ia = None

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
        
        # 📄 PROCESADOR DE PDF (Tablas y texto)
        if ext.endswith('.pdf'):
            try:
                with pdfplumber.open(ruta_completa) as pdf:
                    for numero_pagina, pagina in enumerate(pdf.pages):
                        texto = pagina.extract_text()
                        if texto and texto.strip():
                            doc = Document(
                                page_content=texto,
                                metadata={"source": nombre_archivo, "page": numero_pagina + 1}
                            )
                            documentos_langchain.append(doc)
            except Exception:
                continue
                
        # 📊 PROCESADOR DE XML / XLM
        elif ext.endswith('.xml') or ext.endswith('.xlm'):
            texto_xml = extraer_texto_de_xml(ruta_completa)
            if texto_xml:
                doc = Document(
                    page_content=texto_xml,
                    metadata={"source": nombre_archivo, "page": 1}
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
        "Sos un asistente de IA experto en análisis técnico de documentos (PDFs y XMLs). "
        "Tu único objetivo es responder la pregunta del usuario utilizando de forma estricta "
        "y exclusiva el contexto provisto abajo. No uses conocimientos externos.\n\n"
        "REGLAS OBLIGATORIAS DE RESPUESTA:\n"
        "1. IDIOMA Y TONO: Responde siempre en español de Argentina de forma clara y directa.\n"
        "2. ESTRUCTURA VISUAL: Organiza la información técnica o tablas de datos usando tablas Markdown legibles o viñetas.\n"
        "3. TRATAMIENTO DE LA DUDA: Si la respuesta no figura explícitamente en el contexto, di exactamente: "
        "'Lo siento, no encuentro esa información en los documentos.' No inventes nada bajo ninguna circunstancia.\n\n"
        "Contexto:\n{context}"
    )
    prompt = ChatPromptTemplate.from_messages([("system", instrucciones), ("human", "{input}")])
    cadena_documentos = create_stuff_documents_chain(llm, prompt)
    sistema_ia = create_retrieval_chain(recuperador, cadena_documentos)
    print("🚀 Servidor de IA indexado y listo.")

# ==========================================
# ⚙️ ADMINISTRADOR DE CICLO DE VIDA (LIFESPAN)
# ==========================================
@asynccontextmanager
async def lifespan(app: FastAPI):
    # Corre la inicialización al encender el backend 🚀
    inicializar_sistema()
    yield

# Inicializamos FastAPI usando el manejador moderno de eventos
app = FastAPI(title="API Servidor RAG", lifespan=lifespan)

@app.get("/")
def ruta_raiz():
    """Ruta de prueba que usa la app de C# para verificar la conexión."""
    return {"status": "online"}

@app.post("/preguntar")
def preguntar_ia(consulta: Consulta):
    global sistema_ia
    if not sistema_ia:
        raise HTTPException(status_code=503, detail="El sistema de IA no está inicializado.")
    try:
        resultado = sistema_ia.invoke({"input": consulta.pregunta})
        fuentes = []
        for doc in resultado["context"]:
            archivo = doc.metadata.get("source", "Desconocido")
            pagina = doc.metadata.get("page", 0)
            if archivo != "vacio":
                # Si el archivo es un XML o XLM omitimos el número de página
                if archivo.lower().endswith(('.xml', '.xlm')):
                    fuentes.append(f"• {nombre_archivo}")
                else:
                    fuentes.append(f"• {archivo} (Pág. {pagina})")
        fuentes_unicas = list(set(fuentes))
        return {"respuesta": resultado["answer"], "fuentes": fuentes_unicas}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8000)
