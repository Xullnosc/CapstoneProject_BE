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
    <div style='background-color: #f97316; padding: 20px; text-align: center; color: white;'>
        <h1 style='margin: 0; font-size: 24px;'>Eligibility Notification</h1>
    </div>
    <div style='padding: 30px; background-color: #ffffff;'>
        <p style='font-size: 16px; color: #333;'>Hello,</p>
        <p style='font-size: 16px; color: #555; line-height: 1.5;'>We would like to inform you that you are currently <strong>not eligible</strong> to participate in the Capstone Project for this semester (<strong>{SemesterName}</strong>).</p>
        <p style='font-size: 16px; color: #555; line-height: 1.5;'>If you believe this is a mistake, please contact the Academic Office or your Head of Department (HOD) for assistance.</p>
        <div style='margin: 30px 0; text-align: center;'>
            <a href='{SystemLink}' style='background-color: #f97316; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 16px;'>Access System</a>
        </div>
        <p style='font-size: 14px; color: #777;'>Best regards,<br>FCTMS Team</p>
    </div>
    <div style='background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; color: #999;'>&copy; {CurrentYear} FCTMS. All rights reserved.</div>
</div>";
        public const string WhitelistInvitationTemplate = @"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
    <div style='background-color: #f97316; padding: 20px; text-align: center; color: white;'>
        <h1 style='margin: 0; font-size: 24px;'>Welcome to FCTMS</h1>
    </div>
    <div style='padding: 30px; background-color: #ffffff;'>
        <p style='font-size: 16px; color: #333;'>Dear <strong>{StudentName}</strong>,</p>
        <p style='font-size: 16px; color: #555; line-height: 1.5;'>Congratulations! You are eligible to undertake the Capstone Project for the semester <strong>{SemesterName}</strong>. Your information has been added to the FCTMS.</p>
        <p style='font-size: 16px; color: #555; line-height: 1.5;'>You can now log into the system using your school email to start finding a team or a topic.</p>
        <div style='margin: 30px 0; text-align: center;'>
            <a href='{SystemLink}' style='background-color: #f97316; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 16px;'>Log In Now</a>
        </div>
        <p style='font-size: 14px; color: #777;'>Best regards,<br>FCTMS Team</p>
    </div>
    <div style='background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; color: #999;'>&copy; {CurrentYear} FCTMS. All rights reserved.</div>
</div>";
    }
}
