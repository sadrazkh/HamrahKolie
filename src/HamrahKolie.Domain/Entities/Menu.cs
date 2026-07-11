using HamrahKolie.Domain.Common;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Domain.Entities;

/// <summary>یک منو (سرصفحه یا پاورقی) با آیتم‌های آن.</summary>
public class Menu : BaseEntity
{
    public string Name { get; set; } = default!;
    public MenuLocation Location { get; set; }
    public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
}

/// <summary>یک آیتم منو.</summary>
public class MenuItem : BaseEntity
{
    public long MenuId { get; set; }
    public Menu Menu { get; set; } = default!;

    public string Title { get; set; } = default!;

    /// <summary>نشانی مقصد (نسبی یا کامل).</summary>
    public string Url { get; set; } = "#";

    public int SortOrder { get; set; }

    /// <summary>آیا در تب جدید باز شود؟</summary>
    public bool OpenInNewTab { get; set; }

    public long? ParentId { get; set; }
    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
}
