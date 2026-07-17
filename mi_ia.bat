@echo off
title Lanzador Automático Inteligente
echo 🐍 Encendiendo motor de Python en segundo plano...

cd "C:\Users\Usuario\Desktop\automatizar\mi IA"
start /min py servidor_ia.py

echo 💻 Abriendo interfaz gráfica de Windows...
cd "C:\Users\Usuario\Desktop\automatizar\mi IA\LectorIAPDF\bin\Release\net10.0-windows\win-x64\publish"
start LectorIAPDF.exe

exit
