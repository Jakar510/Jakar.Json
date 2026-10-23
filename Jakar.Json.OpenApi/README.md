# Jakar.Json.OpenApi

OpenAPI documents that describe `[JsonModel]` types with the property names [Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) actually writes.

Both OpenAPI generators name properties from System.Text.Json's options, usually camelCase. A model's own naming can differ: a `[JsonModel(Naming = ...)]` policy, `[assembly: JsonDefaults]`, `[JsonMember(Name = ...)]` / `[JsonKey]`, `[JsonMember(Ignore = true)]` / `[JsonSkip]`, `ReadOnlyMembers = Ignore`, or included non-public members (`[JsonOptIn]`). Without this package, the document describes names your API doesn't use.

```csharp
// Microsoft.AspNetCore.OpenApi
builder.Services.AddOpenApi(o => o.AddJakarJsonSchemas());

// Swashbuckle
builder.Services.AddSwaggerGen();
builder.Services.AddJakarJsonSwagger();
```

Every other type is described as before.

## Keep the attributes

Jakar.Json's attributes are compiled away by default: `[JsonModel]`, `[JsonMember]` and `[JsonDefaults]` are `[Conditional("JAKAR_JSON_KEEP_ATTRIBUTES")]`, because the generator reads them at compile time. Schemas need them at run time. This package defines `JAKAR_JSON_KEEP_ATTRIBUTES` in every project that references it, through `buildTransitive`.

**Models declared in another assembly** (a shared contracts project, for example) need it there too:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);JAKAR_JSON_KEEP_ATTRIBUTES</DefineConstants>
</PropertyGroup>
```

A model compiled without the attributes keeps System.Text.Json's description.

## What's resolved

The rules are the ones the generator applies:

- **Members:** public properties and fields, plus members marked `[JsonMember]`, `[JsonKey]` or `[JsonOptIn]`.
  - Excluded: `[JsonMember(Ignore = true)]`, `[JsonSkip]` (unless `[JsonMember(Ignore = false)]` overrides it), extension data (`[JsonOverflow]`), and, with `ReadOnlyMembers = Ignore` on the model, the assembly or the member, get-only members that no constructor parameter binds.
  - Base members come first. An override appears once.
- **Names**, by precedence:
  1. `[JsonMember(Name)]`, then `[JsonKey]`.
  2. The naming policy, from the first of these that sets one: `[JsonModel(Naming)]`, then `[assembly: JsonDefaults]`. With none, names are as declared.
- **Policies:** camelCase, PascalCase, snake_case and kebab-case, lower and upper, computed exactly as the generator does.

`JsonModelContract.GetMembers(type)` exposes the resolved members if you need them elsewhere.

The package uses reflection, as both OpenAPI generators do, so it isn't marked AOT-compatible. Document generation usually runs at build or design time.
