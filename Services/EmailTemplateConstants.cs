namespace Services
{
    public static class EmailTemplateConstants
    {
        public const string TeamInvitationTemplate = @"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
    <div style='background-color: #f97316; padding: 20px; text-align: center; color: white;'>
        <h1 style='margin: 0; font-size: 24px;'>Team Invitation</h1>
    </div>
    <div style='padding: 30px; background-color: #ffffff;'>
        <p style='font-size: 16px; color: #333;'>Hello <strong>{StudentName}</strong>,</p>
        <p style='font-size: 16px; color: #555; line-height: 1.5;'>You have been invited to join the team <strong>{TeamName}</strong> by <strong>{InviterName}</strong>.</p>
        <div style='margin: 30px 0; text-align: center;'>
            <a href='{InvitationLink}' style='background-color: #f97316; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 16px;'>View Invitation</a>
        </div>
        <p style='font-size: 14px; color: #777;'>Please log in to the FCTMS system to accept or decline this invitation.</p>
    </div>
    <div style='background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; color: #999;'>&copy; {CurrentYear} FCTMS. All rights reserved.</div>
</div>";

        public const string MentorInvitationTemplate = @"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
    <div style='background-color: #0ea5e9; padding: 20px; text-align: center; color: white;'>
        <h1 style='margin: 0; font-size: 24px;'>Mentor Invitation</h1>
    </div>
    <div style='padding: 30px; background-color: #ffffff;'>
        <p style='font-size: 16px; color: #333;'>Dear Lecturer <strong>{MentorName}</strong>,</p>
        <p style='font-size: 16px; color: #555; line-height: 1.5;'>You have been invited to be the mentor for team <strong>{TeamName}</strong> by <strong>{InviterName}</strong>.</p>
        <div style='margin: 30px 0; text-align: center;'>
            <a href='{InvitationLink}' style='background-color: #0ea5e9; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 16px;'>View Request</a>
        </div>
        <p style='font-size: 14px; color: #777;'>Please log in to the FCTMS system using your school email to review this request.</p>
    </div>
    <div style='background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; color: #999;'>&copy; {CurrentYear} FCTMS. All rights reserved.</div>
</div>";
        public const string WhitelistUnqualifiedTemplate = @"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
    <div style='background-color: #ef4444; padding: 20px; text-align: center; color: white;'>
        <h1 style='margin: 0; font-size: 24px;'>Thông Báo Điều Kiện Tham Gia</h1>
    </div>
    <div style='padding: 30px; background-color: #ffffff;'>
        <p style='font-size: 16px; color: #333;'>Chào bạn,</p>
        <p style='font-size: 16px; color: #555; line-height: 1.5;'>Hệ thống xin thông báo bạn hiện <strong>không đủ điều kiện</strong> tham gia làm đồ án tốt nghiệp trong kỳ học này (<strong>{SemesterName}</strong>).</p>
        <p style='font-size: 16px; color: #555; line-height: 1.5;'>Nếu bạn tin rằng đây là một sự nhầm lẫn, vui lòng liên hệ với văn phòng đào tạo hoặc Chủ nhiệm bộ môn (HOD) để được hỗ trợ.</p>
        <div style='margin: 30px 0; text-align: center;'>
            <a href='{SystemLink}' style='background-color: #ef4444; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 16px;'>Truy cập hệ thống</a>
        </div>
        <p style='font-size: 14px; color: #777;'>Trân trọng,<br>Đội ngũ FCTMS</p>
    </div>
    <div style='background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; color: #999;'>&copy; {CurrentYear} FCTMS. All rights reserved.</div>
</div>";
        public const string WhitelistInvitationTemplate = @"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
    <div style='background-color: #22c55e; padding: 20px; text-align: center; color: white;'>
        <h1 style='margin: 0; font-size: 24px;'>Chào Mừng Đến Với FCTMS</h1>
    </div>
    <div style='padding: 30px; background-color: #ffffff;'>
        <p style='font-size: 16px; color: #333;'>Chào <strong>{StudentName}</strong>,</p>
        <p style='font-size: 16px; color: #555; line-height: 1.5;'>Chúc mừng bạn đã đủ điều kiện thực hiện đồ án tốt nghiệp trong kỳ học <strong>{SemesterName}</strong>. Thông tin của bạn đã được thêm vào hệ thống Quản lý Đồ án Tốt nghiệp (FCTMS).</p>
        <p style='font-size: 16px; color: #555; line-height: 1.5;'>Bạn có thể đăng nhập vào hệ thống ngay bây giờ bằng email trường để bắt đầu tìm kiếm nhóm hoặc đề tài.</p>
        <div style='margin: 30px 0; text-align: center;'>
            <a href='{SystemLink}' style='background-color: #22c55e; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 16px;'>Đăng nhập ngay</a>
        </div>
        <p style='font-size: 14px; color: #777;'>Trân trọng,<br>Đội ngũ FCTMS</p>
    </div>
    <div style='background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; color: #999;'>&copy; {CurrentYear} FCTMS. All rights reserved.</div>
</div>";
    }
}
