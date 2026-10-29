using System.Text.Json;
using Nexora.Application.Identity;
using Nexora.Application.Learning;
using Nexora.Infrastructure.Learning;
using Nexora.Infrastructure.Persistence;
using Xunit;
namespace Nexora.UnitTests;

public sealed class LearningPlanContractTests
{
    [Theory]
    [InlineData("Goal",true)] [InlineData("Task",true)] [InlineData("Course",true)]
    [InlineData("Skill",false)] [InlineData("goal",false)] [InlineData("Goal ",false)] [InlineData("",false)] [InlineData("Project",false)]
    public void D06_exact_source_union_rejects_skill_unknown_and_case_coercion(string kind,bool expected) =>
        Assert.Equal(expected,LearningPlanRules.ValidSource(new(kind,Guid.NewGuid())));
    [Fact]
    public void Empty_source_identity_and_null_are_not_sources()
    { Assert.False(LearningPlanRules.ValidSource(new("Goal",Guid.Empty))); Assert.False(LearningPlanRules.ValidSource(null)); }
    [Theory]
    [InlineData("{\"Title\":\"Plan\",\"OwnerId\":\"spoof\"}")]
    [InlineData("{\"Title\":\"Plan\",\"Status\":\"Completed\"}")]
    [InlineData("{\"Title\":\"Plan\",\"schemaVersion\":2}")]
    public void Metadata_rejects_protected_fields_and_unknown_versions(string json) =>
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<LearningPlanMetadata>(json));
    [Fact]
    public void Nested_typed_source_rejects_unknown_properties() => Assert.Throws<JsonException>(()=>
        JsonSerializer.Deserialize<LearningPlanLink>("{\"Source\":{\"Kind\":\"Goal\",\"Id\":\"00000000-0000-0000-0000-000000000001\",\"OwnerId\":\"spoof\"}}"));
    [Fact]
    public void Period_and_text_validation_preserve_optional_dates()
    {
        Assert.True(LearningPlanRules.ValidMetadata(new("Plan",null,null,null)));
        Assert.True(LearningPlanRules.ValidMetadata(new("Plan",new(2026,10,21),new(2026,10,21),null)));
        Assert.False(LearningPlanRules.ValidMetadata(new("Plan",new(2026,10,22),new(2026,10,21),null)));
        Assert.False(LearningPlanRules.ValidMetadata(new(" ",null,null,null)));
        Assert.False(LearningPlanRules.ValidMetadata(new(new string('x',201),null,null,null)));
        Assert.False(LearningPlanRules.ValidMetadata(new("Plan",null,null,new string('x',20001))));
    }
    [Fact]
    public void Reorder_requires_the_exact_distinct_current_set()
    {
        var a=Guid.NewGuid(); var b=Guid.NewGuid(); Guid[] current=[a,b];
        Assert.True(LearningPlanRules.ExactOrder([b,a],current));
        Assert.False(LearningPlanRules.ExactOrder([a,a],current));
        Assert.False(LearningPlanRules.ExactOrder([a],current));
        Assert.False(LearningPlanRules.ExactOrder([a,Guid.NewGuid()],current));
        Assert.False(LearningPlanRules.ExactOrder(null,current));
        Assert.True(LearningPlanRules.ExactOrder([],[]));
    }
    [Theory]
    [InlineData(null)] [InlineData("*")] [InlineData("W/\"AAAAAAAAAAA=\"")] [InlineData("AAAAAAAAAAA=")]
    [InlineData("\"AAAAAAAAAAB=\"")] [InlineData("\"AAAA\"")]
    public void Revision_requires_a_strong_canonical_quoted_SQL_rowversion(string? tag) => Assert.False(LearningPlanRules.TryRevision(tag,out _));
    [Fact]
    public void Canonical_revision_is_accepted() { Assert.True(LearningPlanRules.TryRevision("\"AAAAAAAAAAA=\"",out var value)); Assert.Equal(8,value.Length); }
    [Fact]
    public void Unavailable_and_empty_never_imply_completed_progress()
    {
        Assert.Equal("No items",LearningPlanRules.ProgressLabel(0,0));
        Assert.Equal("Partially known: Known 2 / Unavailable 1",LearningPlanRules.ProgressLabel(2,1));
        Assert.Equal("Known 3 / Unavailable 0",LearningPlanRules.ProgressLabel(3,0));
        var unavailable=SqlLearningPlanSources.Unavailable(); Assert.Null(unavailable.Percent); Assert.Null(unavailable.Title); Assert.Null(unavailable.SourceState);
    }
    [Fact]
    public void Cursor_binds_owner_actor_filter_action_and_detects_tamper()
    {
        var codec=new LearningPlanCursor("unit-only-not-a-production-key"); var scope=new LearningPlanCursor.Scope(Guid.NewGuid(),Guid.NewGuid(),"learning.plan.read","Planned");
        var boundary=new LearningPlanCursor.Boundary(new DateTime(2026,10,21,0,0,0,DateTimeKind.Utc),Guid.NewGuid()); var token=codec.Encode(scope,boundary);
        Assert.Equal(boundary,codec.Decode(scope,token));
        Assert.Null(codec.Decode(scope with { Owner=Guid.NewGuid() },token));
        Assert.Null(codec.Decode(scope with { Actor=Guid.NewGuid() },token));
        Assert.Null(codec.Decode(scope with { Filter="Completed" },token));
        Assert.Null(codec.Decode(scope with { Action="learning.plan.link" },token));
        Assert.Null(codec.Decode(scope,token[..12]+(token[12]=='A'?'B':'A')+token[13..]));
        Assert.Null(codec.Decode(scope,token+"="));
    }
    [Fact]
    public void Default_readiness_denies_every_entry_without_opening_a_SQL_connection()
    {
        var service=new SqlLearningPlanService(new SqlConnectionFactory("Server=127.0.0.1,1;Database=Nexora_Test_inert;Integrated Security=true;TrustServerCertificate=true;Connect Timeout=1"),"unit-secret");
        var actor=new IdentityPrincipal(Guid.NewGuid(),Guid.NewGuid(),"User",DateTimeOffset.UtcNow);
        Assert.Equal(403,service.Get(actor,Guid.NewGuid()).StatusCode);
        Assert.Equal(403,service.List(actor,null,null).StatusCode);
        Assert.Equal(403,service.Sources(actor,"Goal",null).StatusCode);
        Assert.Equal(403,service.Capabilities(actor).StatusCode);
        Assert.Equal(403,service.Create(actor,new("Plan",null,null,null),Guid.NewGuid().ToString(),null).StatusCode);
        Assert.Equal(403,service.Link(actor,Guid.NewGuid(),null,new(new("Goal",Guid.NewGuid()),null),Guid.NewGuid().ToString(),null).StatusCode);
        Assert.Equal(403,service.Unlink(actor,Guid.NewGuid(),Guid.NewGuid(),null,Guid.NewGuid().ToString(),null).StatusCode);
        Assert.Equal(403,service.Reorder(actor,Guid.NewGuid(),null,new([]),Guid.NewGuid().ToString(),null).StatusCode);
        Assert.Equal(403,service.Complete(actor,Guid.NewGuid(),null,new(true),Guid.NewGuid().ToString(),null).StatusCode);
    }
}
