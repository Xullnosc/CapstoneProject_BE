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
                _ => "Global"
            };
        }

        public static string MapIdToCode(int campusId)
        {
            return campusId switch
            {
                1 => "HO",
                2 => "DN",
                3 => "HCM",
                4 => "CT",
                5 => "QN",
                _ => "Global"
            };
        }

        public static int? MapToId(string? campus)
        {
            if (string.IsNullOrWhiteSpace(campus)) return null;

            var normalized = campus.Trim().ToUpper();

            // Try match by Full Name or common codes
            if (normalized.Contains("HÒA LẠC") || normalized == "HL" || normalized == "HO") return 1;
            if (normalized.Contains("ĐÀ NẴNG") || normalized == "DN") return 2;
            if (normalized.Contains("HỒ CHÍ MINH") || normalized == "HCM" || normalized == "SG") return 3;
            if (normalized.Contains("CẦN THƠ") || normalized == "CT") return 4;
            if (normalized.Contains("QUY NHƠN") || normalized == "QN") return 5;

            return null;
        }

        public static class Roles
        {
            public const string Admin = "Admin";
            public const string Lecturer = "Lecturer";
            public const string Student = "Student";
            public const string HOD = "HOD";
        }

        public static class SemesterStatus
        {
            public const string Open = "Open";
            public const string InProgress = "In Progress";
            public const string Closed = "Closed";

            // Backward compatibility
            [Obsolete("Use Open instead")]
            public const string Active = "Active";
            [Obsolete("Use Open instead")]
            public const string Upcoming = "Upcoming";
            [Obsolete("Use InProgress instead")]
            public const string ReviewThesis = "Review Thesis";
            [Obsolete("Use InProgress instead")]
            public const string ReviewMiddle = "Review Middle Semester";

            public static bool IsOpenStage(string? status) =>
                status == Open || status == Active || status == Upcoming;

            public static bool IsLockedStage(string? status) =>
                status == InProgress || status == ReviewThesis || status == ReviewMiddle;

            public static bool IsClosedStage(string? status) =>
                status == Closed;
        }

        public static class ThesisStatus
        {
            public const string Draft = "Draft";
            public const string OnMentorInviting = "On Mentor Inviting";
            public const string Reviewing = "Reviewing";
            public const string Registered = "Registered";
            public const string NeedUpdate = "Need Update";
            public const string Published = "Published";
            public const string Rejected = "Rejected";
            public const string Cancelled = "Cancelled";
        }

        public static class EvaluationChecklistStatus
        {
            public const string Draft = "Draft";
            public const string Submitted = "Submitted";
        }

        public static class TeamStatus
        {
            public const string Pending = "Pending";
            public const string PendingApproval = "Pending"; // Map both to DB "Pending"
            public const string Insufficient = "Insufficient";
            public const string Disbanded = "Disbanded";
            public const string Active = "Qualified"; // Match DB enum
        }

        public static class TeamRole
        {
            public const string Leader = "Leader";
            public const string Member = "Member";
        }

        public static class WhitelistStatus
        {
            public const string Qualified = "Qualified";
            public const string Unqualified = "Unqualified";
        }

        public static class MajorGroupCode
        {
            public const string IT = "IT";
            public const string Biz = "Biz";
            public const string Art = "Art";
            public const string Lan = "Lan";
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

        public static class MentorRecommendationStatus
        {
            public const string Pending = "Pending";
            public const string Recommended = "Recommended";
            public const string Rejected = "Rejected";
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
