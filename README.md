# Sistema de Asistencia y Notas – Instituto Códice

Aplicación de escritorio en C# (Windows Forms, .NET 8) para registrar estudiantes, asistencia y notas.
Versión preliminar desarrollada a partir del diagrama de clases UML de la Subtarea 3B.

## Funcionalidades

| ID | Requerimiento | Pantalla |
|---|---|---|
| RF01 | Agregar estudiante | Estudiantes |
| RF02 | Editar datos del estudiante | Estudiantes |
| RF03 | Registrar asistencia | Asistencia |
| RF04 | Registrar nota | Notas |
| RF05 | Modificar nota | Notas |
| RF06 | Ver notas y asistencias | Consultas |
| RF07 | Calcular promedio por asignatura | Consultas |
| RNF02 | Seguridad: inicio de sesión con roles, contraseñas con hash (PBKDF2-SHA256) y respaldo de datos | Inicio de sesión |
| RNF03 | Búsqueda de estudiantes por nombre, RUT o correo | Estudiantes |
| RNF05 | Persistencia en base de datos SQL (SQLite) | Todas |

Extensión adicional: eliminar estudiantes (junto con sus notas y asistencias), con confirmación previa.

## Requisitos

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (o Visual Studio 2022 con la carga de trabajo «Desarrollo de escritorio .NET»)
- Conexión a internet en la primera compilación (descarga el paquete NuGet `Microsoft.Data.Sqlite`)

## Ejecutar

```bash
dotnet run --project SistemaCodice.csproj
```

O abrir la carpeta en Visual Studio y presionar **F5**.

Para generar un ejecutable único (no requiere .NET instalado en el equipo de destino):

```bash
compilar.bat
```

El resultado queda en `publish\SistemaCodice.exe`.

## Cuentas de prueba

| Perfil | Correo | Contraseña |
|---|---|---|
| Profesor | `profe@codice.cl` | `profesor123` |
| Estudiante | `ana@codice.cl` | `estudiante123` |
| Estudiante | `luis@codice.cl` | `estudiante123` |
| Estudiante | `marta@codice.cl` | `estudiante123` |

Los estudiantes nuevos reciben como contraseña inicial su RUT sin puntos, guion ni dígito verificador.
Son datos de demostración que se crean la primera vez que se ejecuta la aplicación.

## Estructura del proyecto

```
Models/    Clases del dominio: Usuario, Estudiante, Profesor, Asignatura, Nota, Asistencia, Validador
Data/      Persistencia SQLite (AlmacenamientoDatos), contenedor en memoria y datos de ejemplo
Forms/     Interfaz Windows Forms: LoginForm y MainForm
esquema.sql  Script de referencia con las tablas de la base de datos
```

## Base de datos

SQLite, archivo `datos.db` en `%LocalAppData%\InstitutoCodice\`. Se crea automáticamente con datos de ejemplo;
borrar el archivo la restablece. Antes de cada guardado se deja una copia de respaldo (`datos.db.bak`).
El archivo no se versiona (ver `.gitignore`). Las tablas están documentadas en `esquema.sql`.

## Limitaciones de la versión preliminar

- La gestión de asignaturas y profesores no está disponible desde la interfaz.
- No incluye reportes exportables ni pruebas automatizadas.
- `Proteger()` crea un respaldo del archivo; no lo cifra.
