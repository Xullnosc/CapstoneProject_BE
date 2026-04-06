using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class CampusConstants
    {
        public const string HoaLac = "FU-Hòa Lạc";
        public const string HoChiMinh = "FU-Hồ Chí Minh";
        public const string DaNang = "FU-Đà Nẵng";
        public const string CanTho = "FU-Cần Thơ";
        public const string QuyNhon = "FU-Quy Nhơn";

        public static readonly List<string> All = new()
        {
            HoaLac,
            HoChiMinh,
            DaNang,
            CanTho,
            QuyNhon,
        };

        public static string? MapCodeToFullName(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return code;

            return code.ToUpper() switch
            {
                "HO" => HoaLac,
                "HCM" => HoChiMinh,
                "DN" => DaNang,
                "CT" => CanTho,
                "QN" => QuyNhon,
                _ => code // Return as-is if already mapped or unknown
            };
        }

        public static string MapIdToFullName(int campusId)
        {
            return campusId switch
            {
                1 => HoaLac,
                2 => DaNang,
                3 => HoChiMinh,
                4 => CanTho,
                5 => QuyNhon,
                _ => "Unknown"
            };
        }

        public static class Roles
        {
            public const string HOD = "HOD";
            public const string Student = "Student";
            public const string Lecturer = "Lecturer";
            public const string Admin = "Admin";
        }

        public static class InvitationStatus
        {
            public const string Pending = "Pending";
            public const string Accepted = "Accepted";
            public const string Declined = "Declined";
            public const string Cancelled = "Cancelled";
        }

        public static class InvitationType
        {
            public const string Member = "Member";
            public const string Mentor = "Mentor";
        }

        public static class SemesterStatus
        {
            /// <summary>Mở — Thực hiện được tất cả hoạt động nhóm và đề tài.</summary>
            public const string Open = "Open";

            /// <summary>Đang giữa kỳ — Chỉ phục vụ review giữa kỳ. Không tạo nhóm/đề tài.</summary>
            public const string InProgress = "In Progress";

            /// <summary>Đóng — Chỉ xem, không thao tác gì.</summary>
            public const string Closed = "Closed";

            // ── Legacy / Backward compatibility aliases (Giữ để không gãy logic cũ) ──
            [Obsolete("Sử dụng Open thay thế")]
            public const string Active = "Active";
            [Obsolete("Sử dụng Open thay thế")]
            public const string Upcoming = "Upcoming";
            [Obsolete("Sử dụng InProgress thay thế")]
            public const string ReviewThesis = "Review Thesis";
            [Obsolete("Sử dụng InProgress thay thế")]
            public const string ReviewMiddle = "Review Middle Semester";

            /// <summary>Kiểm tra semester có đang ở giai đoạn Mở không (cho phép mọi hoạt động).</summary>
            public static bool IsOpenStage(string? status) =>
                status == Open || status == Active || status == Upcoming;

            /// <summary>Kiểm tra semester đang bị khóa (chỉ review giữa kỳ).</summary>
            public static bool IsLockedStage(string? status) =>
                status == InProgress || status == ReviewThesis || status == ReviewMiddle;

            /// <summary>Kiểm tra semester đã đóng (chỉ xem).</summary>
            public static bool IsClosedStage(string? status) =>
                status == Closed;
        }

        public static class TeamStatus
        {
            public const string Pending = "Pending"; // Matches DB enum
            public const string PendingApproval = "Pending"; // Map both to DB "Pending"
            public const string Insufficient = "Insufficient";
            public const string Disbanded = "Disbanded";
            public const string Active = "Qualified"; // Match DB enum "Qualified"
        }

        public static class TeamRole
        {
            public const string Leader = "Leader";
            public const string Member = "Member";
        }

        public static class WhitelistImportColumns
        {
            public const string Email = "Email";
            public const string StudentCode = "StudentCode";
            public const string FullName = "FullName";
            public const string RoleId = "RoleId";
            public const string Campus = "Campus";
            public const string SemesterId = "SemesterId";
            public const string SemesterCode = "SemesterCode";
        }
    }
}
