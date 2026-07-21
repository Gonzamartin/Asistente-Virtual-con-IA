@echo off
title Lanzador Automático Inteligente
echo 🐍 Configurando entorno y encendiendo motor de Python...

:: 1. Entramos a la carpeta raíz de tu proyecto
cd /d "C:\Users\Usuario\Desktop\automatizar\mi IA"

:: 🛡️ SOLUCIÓN AL CONGELAMIENTO: Forzamos la codificación UTF-8 en el sistema
set PYTHONIOENCODING=utf-8

:: 🚀 EJECUCIÓN AISLADA CON VENV: Llama al Python del entorno virtual que se acaba de instalar
start venv\Scripts\python.exe -m uvicorn servidor_ia:app --host 127.0.0.1 --port 8000 --workers 1


:: Le damos 4 segundos al servidor para que levante los modelos en memoria de tu i3
timeout /t 4 /nobreak >nul

echo 💻 Abriendo interfaz gráfica de Windows...

:: 🎯 RUTA DE COMPILACIÓN CORRECTA DE C#
cd "C:\Users\Usuario\Desktop\automatizar\mi IA\LectorIAPDF\bin\Release\net10.0-windows"
start "" "LectorIAPDF.exe"

exit
