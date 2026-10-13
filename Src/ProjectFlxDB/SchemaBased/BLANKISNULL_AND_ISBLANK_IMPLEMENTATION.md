# blankIsNull / _isBlank Implementation in the ProjFlx Codebase

**Changed by GROK 2026-04-15 | Ref: GROK-202604-StateProgramBlank**

This document is the authoritative map of **every** place inside the pure ProjFlx source that participates in the "clear a field via _isBlank" feature.

> All recent updates related to direct empty-string handling in `fillParms` / `FillParameters` were made by Grok. Search the codebase for "GROK-202604-StateProgramBlank" or "Changed by GROK" to locate all related modifications.

## 1. Schema Definition (the contract)

**File:** `Src/ProjectFlxCommon/Schema/ProjectFlxSql.xsd`

```xml
<xs:attribute name="blankIsNull" type="xs:boolean" default="true" use="optional"/>
```

**Generated code:** `Src/ProjectFlxCommon/Schema/ProjectFlxSql.cs` (partial class `parameter`)

- Private backing field: `private bool blankIsNullField;`
- Ctor default: `this.blankIsNullField = true;`
- Public property with `[System.ComponentModel.DefaultValueAttribute(true)]` + `[XmlAttribute]`

This attribute exists on the `<parameter>` element inside any `<query>` in a ProjectSql document.

## 2. The One Line That Actually Does Something (the entire "implementation")

**File:** `Src/ProjectFlxDB/SchemaBased/DatabaseQuery.cs`, inside `_Query(...)`, the SP parameter-building loop (~line 212 after the 2025 documentation update):

```csharp
if (!parm.blankIsNull && parm.Text.Count == 0)
    parm.Text.Add("");
```

Immediately followed by the skip rule:

```csharp
if (parm.Text.Count == 0 && !isoutparm)
    continue;
```

**Effect:**
- `blankIsNull == true` (default) + empty Text → parameter is **omitted** from the SqlCommand (SP sees no @param → typical update SPs keep the original DB value via ISNULL/CASE).
- `blankIsNull == false` + empty Text → we force `""` so the parameter **is sent** as an explicit empty string (SP can treat `''` as "clear the column to NULL").

## 3. Form / Web Population Paths (now with direct _isBlank → empty string support)

As of the latest changes, the main low-level helpers now actively test for the `_isBlank` convention (by the `paramname + "_isBlank"` key with value `"true"`) and, when found, directly set an explicit empty string `""` for the base parameter.

This matches the design intent: the flag means "pass empty string (special-cased differently from NULL in the SPROC)".

Affected methods:
- `DatabaseQuery.XObject.FillParameters(httpContext, xobj, nv)` — now checks for the flag first and forces `""`.
- `commonProj.fillParms(nv, ...)` — detects keys ending in `_isBlank` and forces the base param to empty string.
- The higher-level MSO `BusinessObjects.SchemaBased.*.fillParms()` / `_fillParmsWeb()` — also updated to directly set `Text = [""]` when the flag is present.

The old `blankIsNull` + injector path remains for compatibility but is no longer the primary mechanism for this use case.

None of these read `xxx_isBlank` or ever set `blankIsNull`:

1. `DatabaseQuery.XObject.FillParameters(httpContext, xobj, nv)` (static) – lines ~1040+
   - Only copies `Form[name]` or `NameValue[name]` into `.Text` when the key exists (including `""`).
   - Explicitly documented (2025) as not understanding the _isBlank convention.

2. Top-level `XObject.FillParameters` in `Src/ProjectFlxDB/XObject.cs:294` (the older DBParameterFields path)
   - Same limitation. Now also explicitly documented.

3. `commonProj.fillParms(object someObject, bool BlankIsNull = true)` in `Src/ProjectFlxCommon/Schema/ProjectFlxSqlExt.cs:648`
   - **Different and inverted semantics.**
   - When `BlankIsNull==true` it *skips* empty values.
   - This is a general-purpose helper, not part of the form-clearing _isBlank feature.

4. Classic `XObject.SetParameter(...)` / `_SetParameter` (mutates XML nodes directly) – zero awareness.

5. `SchemaQueryType.AddParameter(...)` helpers – just create parameters with values.

## 4. Roundtrips That Can Carry the Runtime State

- `parameters.Clone()` (`ProjectFlxSqlExt.cs:79`):
  ```csharp
  public parameters Clone() { var text = this.Serialize(); return Deserialize(text); }
  ```
- Many `IProject.SchemaQuery` getters do `ProjectFlx.Schema.SchemaQueryType.Deserialize( xxx.Serialize() )`.
- The inner `DatabaseQuery.XObject.setQuery` uses `q.parameters.Clone()` on first use (line 992).

Because `blankIsNull` is an XML attribute, a mutation performed *before* any of these roundtrips is preserved in the XML and re-materialized on deserialize. Mutations performed *after* a clone/roundtrip are lost for that copy.

## 5. What ProjFlx Does NOT Do

- ProjFlx never looks at `Request["something_isBlank"]`.
- ProjFlx never calls `p.blankIsNull = false` itself.
- The entire "detect the flag + decide to clear" intelligence lives in the *application* layer (e.g. MSO `BusinessObjects.SchemaBased.Base._fillParmsWeb`).

## 6. Recommended Usage Pattern (for application code)

```csharp
// Application layer (outside ProjFlx)
var p = ...schema parameter for "p_Description"...;
string posted = Request["p_Description"];
string isBlank = Request["p_Description_isBlank"];

if (string.IsNullOrEmpty(posted) && isBlank == "true")
{
    p.blankIsNull = false;
    // leave p.Text.Count == 0  → the injector in DatabaseQuery will turn it into ""
}
else if (!string.IsNullOrEmpty(posted))
{
    p.Text = new List<string> { posted };
}
```

Then pass the `IProject` (or the mutated `SchemaQueryType`) to `DatabaseQuery.Query(...)`.

## 7. History / Design Notes

The comment that used to say "see _fillParmsWeb for implementation details" was misleading — `_fillParmsWeb` has never existed inside ProjFlx. It has always been an MSO-specific method. The 2025 documentation update made the boundary explicit.

---

**Bottom line:** Inside pure ProjFlx the feature is a one-line guard + a schema attribute + documentation of the contract that callers must follow. All the interesting conversion logic from HTML form post to `blankIsNull=false` lives in the host application.
