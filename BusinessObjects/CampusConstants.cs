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
        HoaLac, HoChiMinh, DaNang, CanTho, QuyNhon
        };

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

        public static class TeamStatus
        {
            public const string Pending = "Pending";
            public const string Insufficient = "Insufficient";
            public const string Disbanded = "Disbanded";
            public const string Active = "Active"; // Assuming 'Active' exists or will be used
        }

        public static class TeamRole
        {
            public const string Leader = "Leader";
            public const string Member = "Member";
        }
    }
}
