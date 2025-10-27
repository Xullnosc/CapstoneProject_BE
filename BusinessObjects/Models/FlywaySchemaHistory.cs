using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BusinessObjects.Models;

[Table("flyway_schema_history")]
[Index("Success", Name = "flyway_schema_history_s_idx")]
public partial class FlywaySchemaHistory
{
    [Key]
    [Column("installed_rank")]
    public int InstalledRank { get; set; }

    [Column("version")]
    [StringLength(50)]
    public string? Version { get; set; }

    [Column("description")]
    [StringLength(200)]
    public string? Description { get; set; }

    [Column("type")]
    [StringLength(20)]
    public string Type { get; set; } = null!;

    [Column("script")]
    [StringLength(1000)]
    public string Script { get; set; } = null!;

    [Column("checksum")]
    public int? Checksum { get; set; }

    [Column("installed_by")]
    [StringLength(100)]
    public string InstalledBy { get; set; } = null!;

    [Column("installed_on", TypeName = "datetime")]
    public DateTime InstalledOn { get; set; }

    [Column("execution_time")]
    public int ExecutionTime { get; set; }

    [Column("success")]
    public bool Success { get; set; }
}
