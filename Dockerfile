FROM debian:bookworm-slim

RUN apt-get update && apt-get install -y --no-install-recommends \
        build-essential \
        make \
        dos2unix \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Se COPIA el codigo dentro de la imagen (NO se monta la carpeta de Windows).
# Asi el build ocurre en el filesystem del contenedor (case-sensitive) y nunca
# escribe ni borra en el disco del host. Importante en Windows: el Makefile hace
# 'rm -rf client server' en fclean, y en un volumen montado (case-insensitive)
# eso borraria los directorios Client/ y Server/. Con COPY eso no puede ocurrir.
COPY . .

# El Makefile se edita en Windows (CRLF); GNU make necesita LF. Se normaliza la
# copia interna (la del host no se toca) y se compila server + client.
RUN dos2unix Makefile && make re

# Al arrancar el contenedor, mostrar que los binarios se generaron.
CMD ["sh", "-c", "ls -la server client && echo 'Compilacion Linux OK'"]
