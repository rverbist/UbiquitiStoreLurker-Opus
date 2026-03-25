---
name: c2l-ef6-models
description: 'Design and update modern Coach2Lead EF6 domain models (aggregate roots, children, associations, composition interfaces, and codegen-aware attributes). Use when adding/modifying entities, wiring attachments/comments/status patterns, or preparing model changes for migrations/codegen.'
---

# Coach2Lead EF6 Models

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Objective](#objective)
- [Read First](#read-first)
- [Core Anchors](#core-anchors)
- [Standard Workflow](#standard-workflow)
- [Guardrails](#guardrails)
- [Validation](#validation)
- [Golden Example](#golden-example)
- [Output Contract](#output-contract)
- [Companion Skills](#companion-skills)
- [Skill-Specific Topics](#skill-specific-topics)

</details>
<!-- toc:end -->

## Objective
- Implement model changes that match modern Coach2Lead conventions and codegen expectations.
- Keep entity design compatible with tenant scoping, auditing, and Breeze/API generation.

## Read First
- `docs/knowledgebase/domain/c2l-domain-model-overview.md`
- `docs/knowledgebase/domain/scaffolding/feature-scaffolding-guide.md`

## Core Anchors
- `Coach2Lead/Entity/Base/Entity.cs`
- `Coach2Lead/Entity/Base/AuditedEntity.cs`
- `Coach2Lead/Entity/Interfaces/*`
- `Coach2Lead/Interfaces/IHaveCompany.cs`
- `Coach2Lead/Attributes/CodeGen/*`
- `Coach2Lead/Attributes/SQL/*`
- `Coach2Lead.Web/App/Persistence/ApplicationDbContext*.cs`
- `Coach2Lead.Web/App/Domain/CodeGen/*`

## Standard Workflow
1. Choose model type: aggregate root, aggregate child (`IAggregate<T>`), association (`IAssociation<TLeft, TRight>`), or configuration entity.
2. Implement required base class and interfaces (`AuditedEntity`, `IHaveCompany`, composition interfaces as needed).
3. Add FK + navigation pairs with consistent attribute usage (`Include`/`Exclude`, `ForeignKey`, `SqlDate`, etc.).
4. Add `DbSet<T>` in the correct `ApplicationDbContext` partial.
5. Register save allowances in the module ResourceController/ContextProvider pattern.
6. Add EF6 migration and run generation paths when required.

## Guardrails
- Keep entity properties `virtual` for EF6 proxy/lazy-loading behavior.
- Override all abstract audit properties when inheriting `AuditedEntity`.
- Mark `Company` navigation as `[Exclude]` for `IHaveCompany` entities.
- Use additive schema evolution with migration-first discipline.

## Validation
- Build compiles with model + context updates.
- Migration is generated and applies cleanly when schema changed.
- Breeze metadata and generated query paths include expected model shape.
- Tenant scoping and access interfaces remain coherent with repository queries.

## Golden Example
Minimal aggregate root + child pattern.
```csharp
public class ExampleRoot : AuditedEntity, IHaveCompany, IHaveComments<ExampleComment>
{
    public override int Id { get; set; }
    public virtual int CompanyId { get; set; }
    [Exclude] public virtual Company Company { get; set; }

    [Required]
    public virtual string Name { get; set; }

    public virtual int CommentCount { get; set; }
    public virtual DateTime? LastCommentOn { get; set; }
    [Exclude] public virtual ICollection<ExampleComment> Comments { get; set; }

    public override DateTime CreatedOn { get; set; }
    public override string CreatedBy { get; set; }
    public override DateTime ModifiedOn { get; set; }
    public override string ModifiedBy { get; set; }
    public override Guid Resource { get; set; }
}

public class ExampleComment : EntityComment, IAggregate<ExampleRoot>
{
    public virtual int ExampleRootId { get; set; }
    [Exclude] public virtual ExampleRoot ExampleRoot { get; set; }
}
```

## Output Contract
- Entity type(s) added/updated.
- Interfaces and attributes used.
- Context and save registration updates.
- Migration impact and codegen impact.

## Companion Skills
- `c2l-ef6-migrations`
- `c2l-repository-pattern`
- `c2l-multi-tenancy-guards`

## Skill-Specific Topics
- `EntityAttachment` lifecycle must cover derivative entity, parent `IHaveAttachments<T>` wiring, upload/download path, and client metadata behavior.
- `Include` vs `Exclude` annotations strongly influence generated query shape and payload size.
- `#if METRICS` / `[SqlIgnore]` patterns are used for conditional module inclusion.
