# ProjFlx Framework

ProjFlx is a standalone XML/XSLT-based web framework built on .NET.

It is **not** internal to MSO — it is a separate framework that MSO (and potentially other applications) consume.

## Core Purpose

ProjFlx provides a templating and rendering engine driven by XML data combined with XSLT transformations. It was designed to allow pages, forms, navigation, and data displays to be defined declaratively through XML + XSLT rather than traditional server-side code.

## Key Components

| Component              | Location                          | Output              | Responsibility |
|------------------------|-----------------------------------|---------------------|----------------|
| **ProjectFlxCommon**   | `Src/ProjectFlxCommon/`           | `ProjectFlxCommon.dll` | Utilities, XML/XSL helpers, exceptions, configuration, web helpers |
| **ProjectFlxDB**       | `Src/ProjectFlxDB/`               | `ProjectFlxDB.dll`     | Schema-based database querying layer |
| **TMPLTSite / FLX**    | `Src/TMPLTSite/`                  | `FLX.dll`              | Core page rendering, templating engine, resource resolution, XSLT pipeline |
| **Web Assets**         | `Web/ProjectFlx/`                 | —                      | Core XSLT templates (`WBT_*.xsl`, `WEB_DOCUMENT_TEMPLATE.xsl`), Bootstrap integration, sample pages |

## How It Works (High Level)

1. Requests are handled through `FlxWebSite` / `XMLTemplater` / `WebTemplater`.
2. Data is typically loaded into XML (from database via the schema/query system or other sources).
3. XSLT stylesheets transform that XML into final HTML (or other output).
4. A large set of reusable XSLT modules exist for common UI patterns:
   - Tables, forms, paging, links, site maps, styles, scripts, etc.
5. Resources (XML + XSL pairs) are resolved at runtime from the filesystem or embedded sources.

## Consumption Model

Applications do **not** reference the ProjFlx source projects directly in most cases.

Instead, they reference pre-built assemblies from the `Compile/` folder:

- `FLX.dll`
- `ProjectFlxCommon.dll`
- `ProjectFlxDB.dll`

This is visible in MSO, which references them via relative paths such as:

```
..\..\..\ProjFlx\Compile\FLX.dll
```

## Technology Characteristics

- **Target Framework**: .NET Framework 4.8 (and earlier)
- **Rendering**: Heavily XSLT-based (Saxon-HE is included in some builds)
- **Styling**: Early Twitter Bootstrap integration (v3.x and v5.x prototypes exist)
- **Database Access**: Custom schema-driven query model (`ProjectFlxSql.xsd`)
- **Age**: Mature / legacy codebase with significant accumulated complexity

## Strengths

- Powerful declarative templating once understood
- Strong separation between data (XML) and presentation (XSLT)
- Reusable UI component library via XSLT includes

## Challenges

- Steep learning curve for developers unfamiliar with heavy XSLT
- Debugging can be difficult (XSLT transformations, resource resolution)
- Tight coupling between XML structure expectations and stylesheets
- Limited modern tooling / IDE support for the XSLT-heavy patterns

## Relationship to MSO

MSO is the primary (and possibly only remaining) significant consumer of ProjFlx.

MSO depends on the framework for its web rendering and certain data access patterns, while adding its own business logic on top (primarily in `msoCommon` and the various `mso_*` modules).

---

**Status**: This document is a living overview. It was created to preserve architectural understanding of the framework as a distinct artifact.
