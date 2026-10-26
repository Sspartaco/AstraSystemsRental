# Sitio público de Codalea

Sitio de `www.codalea.com.co`: presenta los dos productos de Codalea, AstraSystems (flotas y alquiler de vehículos) y Codalea Comercio (talleres y comercios). HTML, CSS y JavaScript sin build; el contenido está escrito en las páginas, así que se lee completo aunque JavaScript no cargue.

## Estructura

```
web/
├── index.html              Portada, AstraSystems, Codalea Comercio, cómo trabajamos y contacto
├── privacidad.html         Política de tratamiento de datos (Ley 1581 de 2012)
├── terminos.html           Términos de uso del sitio
├── 404.html                Página no encontrada
├── css/sitio.css           Estilos del sitio
├── css/app-preview.css     Simulador de la app de AstraSystems
├── js/sitio.js             Menú, selector de operación, visor de capturas y formulario
├── js/app-preview.js       Simulador de la app (datos de ejemplo)
├── assets/                 Logos, favicon, capturas reales e imagen para compartir (og.jpg)
├── api/                    Función que recibe el formulario y lo envía por correo
├── staticwebapp.config.json  Rutas, 404 y encabezados de seguridad
├── robots.txt, sitemap.xml
```

## Formulario de contacto

`api/src/functions/solicitud.js` recibe `POST /api/solicitud`, valida los datos, exige la autorización de tratamiento de datos, descarta envíos del campo trampa, limita a 5 envíos por conexión cada 10 minutos y manda el correo a `contacto@codalea.com.co` por SMTP de Azure Communication Services. El sitio solo muestra «Recibido» cuando la función confirma el envío; si falla, ofrece WhatsApp y correo.

Configuración de la función (Static Web Apps → Variables de entorno):

| Variable | Valor |
|---|---|
| `SMTP_USUARIO` | Usuario SMTP de Communication Services |
| `SMTP_CLAVE` | Secreto de la aplicación de Entra `appmvp-smtp` |
| `CORREO_DESTINO` | Opcional; por defecto `contacto@codalea.com.co` |

Pruebas: `cd web/api && npm install && npm test`.

## Capturas

Las capturas de Codalea Comercio (`assets/img/comercio/`) se toman del ambiente local con el negocio de demostración, en tema oscuro. Las de AstraSystems están en `assets/img/product/`. No usar datos de clientes reales.

## Desarrollo local

```bash
python -m http.server 8090 --directory web
```

El formulario necesita la función; en local responde error y muestra las alternativas de contacto.

## Publicación

`.github/workflows/sitio-web.yml` publica en Azure Static Web Apps (`codalea-sitio`) cada push a `main` que toque `web/`. Solo se publican los archivos del sitio; esta guía y los documentos internos quedan fuera.
