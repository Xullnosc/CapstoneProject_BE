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

        public const string WhitelistNotificationTemplate = @"
<div style='font-family: ""Inter"", ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 20px auto; border: 1px solid #f1f5f9; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);'>
    <div style='background: linear-gradient(135deg, #f97316 0%, #fb923c 100%); padding: 40px 20px; text-align: center; color: white;'>
        <div style='font-size: 48px; margin-bottom: 15px;'>🎉</div>
        <h1 style='margin: 0; font-size: 26px; font-weight: 800; letter-spacing: -0.025em;'>FCTMS Qualification</h1>
        <p style='margin-top: 10px; opacity: 0.9; font-size: 16px;'>System Access Confirmed</p>
    </div>
    <div style='padding: 40px; background-color: #ffffff;'>
        <p style='font-size: 18px; color: #1e293b; margin-bottom: 24px;'>Hello <strong>{UserName}</strong>,</p>
        <p style='font-size: 16px; color: #4b5563; line-height: 1.8; margin-bottom: 35px;'>
            We are excited to inform you that your profile has been successfully qualified for the <strong>{SemesterName}</strong> semester. 
            You are now officially recognized to participate in the FPT Capstone Project Management System.
        </p>

        <div style='text-align: center; margin-top: 35px;'>
            <a href='{SystemLink}' style='background-color: #f97316; color: white; padding: 16px 32px; text-decoration: none; border-radius: 10px; font-weight: 700; font-size: 16px; display: inline-block; transition: all 0.3s ease; box-shadow: 0 4px 6px -1px rgba(249, 115, 22, 0.2);'>Get Started with FCTMS</a>
        </div>
        
        <div style='margin-top: 40px; border-top: 1px solid #f1f5f9; padding-top: 30px;'>
            <p style='font-size: 13px; color: #94a3b8; line-height: 1.6;'>
                <strong>Quick Tip:</strong> Use your official university email account to sign in. If you encounter any technical issues, please reach out to your administrator via the support portal.
            </p>
        </div>
    </div>
    <div style='background-color: #f8fafc; padding: 25px; text-align: center; border-top: 1px solid #f1f5f9;'>
        <p style='font-size: 12px; color: #64748b; margin: 0; font-weight: 500;'>&copy; {CurrentYear} FCTMS - Capstone Management Solution.</p>
        <p style='font-size: 11px; color: #94a3b8; margin-top: 5px;'>FPT University - Excellence in Technology</p>
    </div>
</div>";
    }
}
