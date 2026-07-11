using HamrahKolie.Domain.Common;

namespace HamrahKolie.Domain.Entities;

/// <summary>دسته‌بندی محتوا.</summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }
    public long? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<Content> Contents { get; set; } = new List<Content>();
}

/// <summary>برچسب محتوا.</summary>
public class Tag : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public ICollection<ContentTag> ContentTags { get; set; } = new List<ContentTag>();
}

/// <summary>رابطه چند-به-چند محتوا و برچسب.</summary>
public class ContentTag
{
    public long ContentId { get; set; }
    public Content Content { get; set; } = default!;
    public long TagId { get; set; }
    public Tag Tag { get; set; } = default!;
}
