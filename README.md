
# 💬 Asistente de IA para PDFs & XMLs (Arquitectura RAG Local-Nube)


Un sistema inteligente de escritorio nativo para Windows que permite chatear y realizar consultas técnicas complejas sobre múltiples archivos **PDF** (con extracción precisa de tablas) y documentos **XML / XLM** en simultáneo.

Desarrollado con una arquitectura **Cliente-Servidor** altamente eficiente, diseñada específicamente para funcionar con un rendimiento instantáneo en hardware liviano (probado en procesadores Intel Core i3) al delegar la inferencia pesada a la nube.





<img width="800" height="300" alt="mi pc" src="https://github.com/user-attachments/assets/f9afb095-09c4-4c5e-b4e0-1db3fb342d89" />

---
### Interfaz del Usuario y Excel con datos solicitados

<p float="left">
  <img src="https://github.com/user-attachments/assets/f30feb3e-68b6-4b38-80cc-f6cc97c504ab" width="45%" alt="Interfaz del Usuario" />
  <img src="https://github.com/user-attachments/assets/2a58bade-4c75-4877-b42f-0836c69fe941" width="45%" alt="Excel con datos solicitados" />
</p>





## 🏗️ Arquitectura del Sistema

El software se divide en dos componentes independientes que se comunican de forma local mediante peticiones HTTP:

1. **Backend (Servidor Python):** Una API ultra liviana construida con **FastAPI**. Utiliza **pdfplumber** para leer texto y celdas de tablas sin pérdidas, genera embeddings semánticos locales con **HuggingFace** (`all-MiniLM-L6-v2`) y almacena los vectores en un disco local mediante **Chroma**. Las consultas se resuelven en milisegundos usando el modelo `llama-3.1-8b-instant` en los servidores remotos de **Groq**.
2. **Frontend (Interfaz de Escritorio C#):** Una aplicación nativa de Windows diseñada en **WPF** compilada en un único archivo ejecutable portátil (`.exe`). Ofrece una interfaz limpia y asíncrona que incluye el panel de respuestas de la IA y un visor de trazabilidad que indica qué archivos y páginas exactas se consultaron.




<img width="450" height="300" alt="Groq" src="https://github.com/user-attachments/assets/ea9f1181-14de-4e9c-b279-12097aeca599" />
<img width="450" height="300" alt="RAGlocal" src="https://github.com/user-attachments/assets/ba9f9b19-d879-443e-9ee9-3025efbdc488" />


---

## ⚡ Características Clave

* **Indexación Inteligente en Disco (Optimizado para i3):** El backend detecta si los documentos ya fueron procesados. Si la base de datos existe, salta el escaneo y arranca en menos de 3 segundos.
* **Procesamiento de Tablas y Estructuras:** Gracias a `pdfplumber`, el sistema extrae celdas y datos tabulares complejos (como cuadros de identificación de suelos o dosis) que los lectores tradicionales ignoran.
* **Soporte Híbrido (PDF + XML):** Cruza información de manuales físicos y archivos de datos estructurados en la misma consulta.
* **Filtro Estricto de Alucinaciones:** El prompt del sistema está optimizado para responder estrictamente con el contexto provisto. Si la información no está en los archivos, la IA responderá: *"Lo siento, no encuentro esa información en los documentos"*.
* **Ejecutable de un solo clic:** Incluye un lanzador automatizado de Windows (`.bat`) que enciende el motor de Python en segundo plano e inicia la interfaz gráfica en simultáneo.

---

## 🛠️ Requisitos e Instalación

### 1. Clonar el repositorio
```bash
git clone https://github.com
cd TU_REPOSITORIO
```

### 2. Configurar el Backend (Python)
Instalá las dependencias del motor de Inteligencia Artificial:
```bash
pip install fastapi uvicorn pypdf pdfplumber langchain-core langchain-groq langchain-huggingface langchain-chroma langchain-classic sentence-transformers
```
> 🔑 **Nota:** Asegurate de configurar tu `GROQ_API_KEY` en las variables de entorno o directamente en la línea 17 del script `servidor_ia.py`.

### 3. Configurar el Frontend (C# / .NET 10)
Asegurate de tener el SDK de .NET instalado y añade el paquete de comunicación JSON:
```bash
cd LectorIAPDF
dotnet add package Newtonsoft.Json
```

Para compilar el archivo ejecutable portable definitivo, ejecutá:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

---

## 🚀 Cómo usar el sistema

1. Guardá todos tus manuales o planillas dentro de la carpeta `mis_pdfs` en el directorio del proyecto.
2. Copiá el archivo ejecutable `Arrancar_IA.bat` en tu Escritorio de Windows.
3. Dale doble clic al `.bat`.
4. La ventana de C# se abrirá de inmediato y se mantendrá a la espera de forma segura (`⏳ Esperando al servidor de Python...`).
5. En cuanto el motor de Python termine de levantar la base de datos, la interfaz se desbloqueará sola y podrás chatear libremente con tus documentos.

> 🔄 **Importante:** Cada vez que agregues, quites o modifiques archivos dentro de la carpeta `mis_pdfs`, borrá la carpeta amarilla `base_datos_chroma_multi` para obligar al sistema a re-indexar los nuevos datos por única vez.
