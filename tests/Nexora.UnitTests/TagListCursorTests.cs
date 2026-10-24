using Nexora.Infrastructure.Organization;
using Xunit;

namespace Nexora.UnitTests;

public sealed class TagListCursorTests
{
    [Fact]
    public void Cursor_preserves_Unicode_name_and_ID_only_for_exact_owner_filters_and_key()
    {
        var cursor = new TagListCursor("synthetic-cursor-key");
        var owner = Guid.NewGuid(); var id = Guid.NewGuid();
        var token = cursor.Encode(owner, "projects", "Việt %_[\\", 25, true, "Kỹ năng 🚀", id);
        Assert.True(cursor.TryDecode(token, owner, "projects", "Việt %_[\\", 25, true, out var name, out var actualId));
        Assert.Equal("Kỹ năng 🚀", name); Assert.Equal(id, actualId);
        Assert.DoesNotContain("Kỹ năng", token);
        Assert.False(cursor.TryDecode(token, owner, "projects", "Việt %_[\\", 25, false, out _, out _));
        Assert.False(cursor.TryDecode(token, Guid.NewGuid(), "projects", "Việt %_[\\", 25, true, out _, out _));
        Assert.False(cursor.TryDecode(token, owner, "documents", "Việt %_[\\", 25, true, out _, out _));
        Assert.False(cursor.TryDecode(token, owner, "projects", null, 25, true, out _, out _));
        Assert.False(cursor.TryDecode(token, owner, "projects", "Việt %_[\\", 50, true, out _, out _));
        Assert.False(new TagListCursor("different-key").TryDecode(token, owner, "projects", "Việt %_[\\", 25, true, out _, out _));
    }

    [Fact]
    public void Tampered_malformed_and_noncanonical_cursors_fail_without_throwing()
    {
        var cursor = new TagListCursor("synthetic-cursor-key"); var owner = Guid.NewGuid();
        var token = cursor.Encode(owner, null, null, 25, false, "tag", Guid.NewGuid());
        var tampered = token[..10] + (token[10] == 'A' ? 'B' : 'A') + token[11..];
        foreach (var value in new[] { "", "tl2.%%%", new string('x', 1025), token + "=", tampered })
            Assert.False(cursor.TryDecode(value, owner, null, null, 25, false, out _, out _));
    }
}
