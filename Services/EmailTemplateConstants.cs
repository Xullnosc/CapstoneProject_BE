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
    }
}
