using GlueFramework.Core.ORM;
using System.ComponentModel.DataAnnotations;

namespace GlueFramework.CustomSysSettingsModule.DataModels
{
    [DataTable("CustomSiteSettings")]
    public class CustomSiteSettings: ModelBase
    {
        [MaxLength(100)]
        public string SKey { get; set; }
        [MaxLength(500)]
        public string? SValue { get; set; }

        public string Group { get; set; }
        public bool DefaultVisible { get; set; } = true;
        public bool Removable { get; set; }

        public bool ReadOnly { get; set; }
    }
    
}
