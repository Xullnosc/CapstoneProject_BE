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
        public const string MidtermAnnouncementTemplate = @"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
    <div style='background: linear-gradient(135deg, #f59e0b, #f97316); padding: 30px 20px; text-align: center; color: white;'>
        <div style='font-size: 40px; margin-bottom: 8px;'>&#9201;</div>
        <h1 style='margin: 0; font-size: 22px; font-weight: 700;'>Midterm Review Announcement</h1>
        <p style='margin: 6px 0 0; font-size: 14px; opacity: 0.9;'>Semester {SemesterCode}</p>
    </div>
    <div style='padding: 30px; background-color: #ffffff;'>
        <p style='font-size: 16px; color: #333; margin-top: 0;'>Dear FCTMS Members,</p>
        <p style='font-size: 15px; color: #555; line-height: 1.6;'>This is an official notice that the system will be <strong>locked for Midterm Review</strong> on the date below. Please ensure all your work is submitted before the deadline.</p>
        <div style='background-color: #fff7ed; border-left: 4px solid #f97316; border-radius: 4px; padding: 16px 20px; margin: 24px 0;'>
            <p style='margin: 0 0 4px; font-size: 13px; color: #9a3412; text-transform: uppercase; font-weight: 600; letter-spacing: 0.5px;'>Lock Date</p>
            <p style='margin: 0; font-size: 20px; color: #c2410c; font-weight: 700;'>{LockDate}</p>
        </div>
        <p style='font-size: 15px; color: #555; line-height: 1.6;'>After this date, the semester will transition to the Midterm Review phase. No further submissions or changes will be accepted.</p>
        <div style='margin: 28px 0; text-align: center;'>
            <a href='{SystemLink}' style='background-color: #f97316; color: white; padding: 14px 32px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 15px; display: inline-block;'>Go to FCTMS</a>
        </div>
        <p style='font-size: 14px; color: #777;'>Best regards,<br><strong>FCTMS Team</strong></p>
    </div>
    <div style='background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; color: #999;'>&copy; {CurrentYear} FCTMS &mdash; FPT Capstone Topic Management System</div>
</div>";

        public const string SemesterCloseTemplate = @"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
    <div style='background: linear-gradient(135deg, #10b981, #059669); padding: 30px 20px; text-align: center; color: white;'>
        <div style='font-size: 40px; margin-bottom: 8px;'>&#127891;</div>
        <h1 style='margin: 0; font-size: 22px; font-weight: 700;'>Semester Closed</h1>
        <p style='margin: 6px 0 0; font-size: 14px; opacity: 0.9;'>Semester {SemesterCode}</p>
    </div>
    <div style='padding: 30px; background-color: #ffffff;'>
        <p style='font-size: 16px; color: #333; margin-top: 0;'>Dear FCTMS Members,</p>
        <p style='font-size: 15px; color: #555; line-height: 1.6;'>Semester <strong>{SemesterCode}</strong> has officially ended. Congratulations on completing the semester!</p>
        <div style='background-color: #ecfdf5; border-left: 4px solid #10b981; border-radius: 4px; padding: 16px 20px; margin: 24px 0;'>
            <p style='margin: 0 0 4px; font-size: 13px; color: #065f46; text-transform: uppercase; font-weight: 600; letter-spacing: 0.5px;'>Status</p>
            <p style='margin: 0; font-size: 20px; color: #047857; font-weight: 700;'>Semester Completed &#10003;</p>
        </div>
        <p style='font-size: 15px; color: #555; line-height: 1.6;'>All data for this semester is now in read-only mode. You can still view your results, thesis details, and team information in the system.</p>
        <div style='margin: 28px 0; text-align: center;'>
            <a href='{SystemLink}' style='background-color: #059669; color: white; padding: 14px 32px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 15px; display: inline-block;'>View Results</a>
        </div>
        <p style='font-size: 14px; color: #777;'>Thank you for being part of FCTMS this semester.<br><strong>FCTMS Team</strong></p>
    </div>
    <div style='background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; color: #999;'>&copy; {CurrentYear} FCTMS &mdash; FPT Capstone Topic Management System</div>
</div>";

        public const string SubmissionLockedTemplate = @"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
    <div style='background: linear-gradient(135deg, #6366f1, #4f46e5); padding: 30px 20px; text-align: center; color: white;'>
        <div style='font-size: 40px; margin-bottom: 8px;'>&#128274;</div>
        <h1 style='margin: 0; font-size: 22px; font-weight: 700;'>Submissions Locked</h1>
        <p style='margin: 6px 0 0; font-size: 14px; opacity: 0.9;'>Semester {SemesterCode}</p>
    </div>
    <div style='padding: 30px; background-color: #ffffff;'>
        <p style='font-size: 16px; color: #333; margin-top: 0;'>Dear FCTMS Members,</p>
        <p style='font-size: 15px; color: #555; line-height: 1.6;'>Topic registration for semester <strong>{SemesterCode}</strong> has been <strong>locked</strong>. The semester is now in the <em>In Progress</em> phase.</p>
        <div style='background-color: #eef2ff; border-left: 4px solid #6366f1; border-radius: 4px; padding: 16px 20px; margin: 24px 0;'>
            <p style='margin: 0 0 4px; font-size: 13px; color: #3730a3; text-transform: uppercase; font-weight: 600; letter-spacing: 0.5px;'>What This Means</p>
            <p style='margin: 0; font-size: 15px; color: #4338ca; line-height: 1.5;'>Teams that have successfully registered will now move into the project execution phase. No new registrations are accepted.</p>
        </div>
        <div style='margin: 28px 0; text-align: center;'>
            <a href='{SystemLink}' style='background-color: #4f46e5; color: white; padding: 14px 32px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 15px; display: inline-block;'>Go to FCTMS</a>
        </div>
        <p style='font-size: 14px; color: #777;'>Best regards,<br><strong>FCTMS Team</strong></p>
    </div>
    <div style='background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; color: #999;'>&copy; {CurrentYear} FCTMS &mdash; FPT Capstone Topic Management System</div>
</div>";

        public const string MidtermLockReminderTemplate = @"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
    <div style='background: linear-gradient(135deg, #ef4444, #dc2626); padding: 30px 20px; text-align: center; color: white;'>
        <div style='font-size: 40px; margin-bottom: 8px;'>&#128680;</div>
        <h1 style='margin: 0; font-size: 22px; font-weight: 700;'>Action Required: Lock Semester</h1>
        <p style='margin: 6px 0 0; font-size: 14px; opacity: 0.9;'>Semester {SemesterCode}</p>
    </div>
    <div style='padding: 30px; background-color: #ffffff;'>
        <p style='font-size: 16px; color: #333; margin-top: 0;'>Dear Head of Department,</p>
        <p style='font-size: 15px; color: #555; line-height: 1.6;'>Today is the scheduled lock date for semester <strong>{SemesterCode}</strong> for Midterm Review. Please access the system and perform the manual lock to finalize the semester transition.</p>
        <div style='background-color: #fef2f2; border-left: 4px solid #ef4444; border-radius: 4px; padding: 16px 20px; margin: 24px 0;'>
            <p style='margin: 0 0 4px; font-size: 13px; color: #991b1b; text-transform: uppercase; font-weight: 600; letter-spacing: 0.5px;'>Action Needed</p>
            <p style='margin: 0; font-size: 15px; color: #b91c1c; line-height: 1.5;'>Log in to FCTMS and lock the semester to begin Midterm Review.</p>
        </div>
        <div style='margin: 28px 0; text-align: center;'>
            <a href='{SystemLink}' style='background-color: #dc2626; color: white; padding: 14px 32px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 15px; display: inline-block;'>Lock Semester Now</a>
        </div>
        <p style='font-size: 14px; color: #777;'>Best regards,<br><strong>FCTMS Team</strong></p>
    </div>
    <div style='background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; color: #999;'>&copy; {CurrentYear} FCTMS &mdash; FPT Capstone Topic Management System</div>
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
