import os
import sys
from pypdf import PdfReader
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
os.environ["GROQ_API_KEY"] = "Aca Va LA APIKEY" 

# 📂 Indicamos la carpeta del escritorio donde vas a guardar tus PDFs
CARPETA_PDFS = r"C:\Users\Usuario\Desktop\automatizar\mi IA\mis_pdfs" 
CARPETA_VECTORIAL = "./base_datos_chroma_multi"

def cargar_multiples_pdfs(ruta_carpeta):
    """Busca y lee todos los archivos .pdf dentro de la carpeta configurada."""
    if not os.path.exists(ruta_carpeta):
        # Si la carpeta no existe, la creamos automáticamente para ayudar al usuario
        os.makedirs(ruta_carpeta)
        print(f"\n📁 Se creó la carpeta en: {ruta_carpeta}")
        print("Por favor, guarda tus archivos PDF ahí dentro y vuelve a ejecutar el script.")
        sys.exit(0)
        
    documentos_langchain = []
    # Listamos todos los archivos del directorio
    archivos = [f for f in os.listdir(ruta_carpeta) if f.lower().endswith('.pdf')]
    
    if not archivos:
        print(f"\n⚠️ La carpeta '{ruta_carpeta}' está vacía. Poné al menos un PDF adentro.")
        sys.exit(0)
        
    print(f"📚 Encontrados {len(archivos)} archivos PDF para procesar...")
    
    for nombre_archivo in archivos:
        ruta_completa = os.path.join(ruta_carpeta, nombre_archivo)
        print(f"📄 Leyendo: {nombre_archivo}")
        
        try:
            lector = PdfReader(ruta_completa)
            for numero_pagina, pagina in enumerate(lector.pages):
                texto = pagina.extract_text()
                if texto and texto.strip():
                    # Guardamos el texto y registramos el nombre del archivo de origen en los metadatos
                    doc = Document(
                        page_content=texto,
                        metadata={"source": nombre_archivo, "page": numero_pagina}
                    )
                    documentos_langchain.append(doc)
        except Exception as e:
            print(f"❌ No se pudo leer el archivo {nombre_archivo}. Error: {e}")
            
    return documentos_langchain

def inicializar_sistema_rag_multi():
    """Inicializa los vectores unificados y la conexión con Groq."""
    print("🧠 Inicializando modelo de lenguaje para procesamiento de texto...")
    embeddings = HuggingFaceEmbeddings(model_name="sentence-transformers/all-MiniLM-L6-v2")
    
    if os.path.exists(CARPETA_VECTORIAL):
        print("📚 Reutilizando base de datos multi-documento indexada en el disco...")
        base_datos = Chroma(
            persist_directory=CARPETA_VECTORIAL, 
            embedding_function=embeddings
        )
    else:
        print("⏳ Iniciando procesamiento de documentos por primera vez...")
        documentos = cargar_multiples_pdfs(CARPETA_PDFS)
        
        separador = RecursiveCharacterTextSplitter(chunk_size=1000, chunk_overlap=200)
        fragmentos = separador.split_documents(documentos)
        
        print(f"💾 Guardando {len(fragmentos)} fragmentos vectoriales en disco con Chroma...")
        base_datos = Chroma.from_documents(
            documents=fragmentos, 
            embedding=embeddings, 
            persist_directory=CARPETA_VECTORIAL
        )
        print("✅ ¡Todos los documentos fueron indexados con éxito!")

    # El recuperador ahora buscará los 5 párrafos más relevantes entre TODOS los archivos
    recuperador = base_datos.as_retriever(search_kwargs={"k": 5})

    print("🤖 Conectando con los servidores de Groq...")
    llm = ChatGroq(
        model="llama-3.1-8b-instant", 
        temperature=0.1
    )

    instrucciones = (
        "Sos un asistente de Inteligencia Artificial experto en análisis técnico de múltiples documentos simultáneos. "
        "Tu único objetivo es responder la pregunta del usuario utilizando de forma estricta "
        "y exclusiva el contexto provisto abajo. No uses conocimientos externos.\n\n"
        
        "REGLAS OBLIGATORIAS DE RESPUESTA:\n"
        "1. IDIOMA Y TONO: Responde siempre en español de Argentina, de manera clara, profesional y directa.\n"
        "2. ESTRUCTURA VISUAL: Si la respuesta contiene datos técnicos, variables, dosis o químicos, "
        "organizá la información usando viñetas (puntos) o negritas para facilitar su lectura.\n"
        "3. TRATAMIENTO DE LA DUDA: Si la respuesta no figura explícitamente en los documentos, "
        "di exactamente: 'Lo siento, no encuentro esa información en los documentos provistos.' No inventes nada.\n"
        "4. PRECISIÓN CIENTÍFICA: Mantén con absoluta exactitud las cifras y unidades de medida.\n\n"
        
        "Contexto extraído de los PDFs:\n{context}"
    )
    
    prompt = ChatPromptTemplate.from_messages([
        ("system", instrucciones),
        ("human", "{input}"),
    ])

    cadena_documentos = create_stuff_documents_chain(llm, prompt)
    return create_retrieval_chain(recuperador, cadena_documentos)

# ==========================================
# FLUJO INTERACTIVO PRINCIPAL
# ==========================================
if __name__ == "__main__":
    try:
        sistema_ia = inicializar_sistema_rag_multi()
        print("\n🚀 ¡Sistema Multi-PDF Activo! Listo para responder sobre cualquier documento de la carpeta.\n")
        
        while True:
            pregunta = input("Haz una pregunta global (o escribe 'salir'): ")
            if pregunta.lower() == 'salir':
                print("¡Hasta luego!")
                break
            
            if not pregunta.strip():
                continue
                
            print("⚡ Buscando en todos los PDFs y consultando a Groq...", end="\r")
            resultado = sistema_ia.invoke({"input": pregunta})
            
            print("\n✨ RESPUESTA DE LA IA (Groq):")
            print(resultado["answer"])
            
            # Trazabilidad avanzada: Te dice el nombre de qué archivo y qué página usó
            print("\n📊 Documentos y páginas consultadas para esta respuesta:")
            fuentes_utilizadas = set()
            for doc in resultado["context"]:
                archivo = doc.metadata.get("source", "Desconocido")
                pagina = doc.metadata.get("page", 0) + 1
                fuentes_utilizadas.add(f"• {archivo} (Pág. {pagina})")
            
            for fuente in sorted(fuentes_utilizadas):
                print(fuente)
            print("-" * 60 + "\n")
            
    except Exception as e:
        print(f"\n❌ Se produjo un error inesperado: {e}")
