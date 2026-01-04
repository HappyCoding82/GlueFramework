using System.ComponentModel.DataAnnotations;

namespace CustomSiteSettingsModule.Dtos
{
    public class SiteSettingDto
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string SKey { get; set; } = string.Empty;
        [MaxLength(500)]
        public string SValue { get; set; } = string.Empty;

        public string Group { get; set; } = string.Empty;
        public bool DefaultVisible { get; set; } = true;

        public bool ReadOnly { get; set; }

        public bool Removable { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }
}
