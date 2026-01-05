using GlueFramework.Core.ORM;
using System.ComponentModel.DataAnnotations;

namespace GlueFramework.CustomSysSettingsModule.DataModels
{
    public class ModelBase
    {
        [Key]
        [DBField("Id", true, true)]
        public int ID { get; set; }
        [MaxLength(50)]
        public string CreatedBy { get; set; } = string.Empty;
        [MaxLength(50)]
        public string LastModifiedBy { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }
}
