namespace MediBook.Services.Email;

public static class EmailTemplateService
{
    public static string AppointmentConfirmation(string patientName, string doctorName, string clinicName, string dateText, string timeText, string reason, double fee)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: Arial, sans-serif; background: #F3F7FC; margin: 0; padding: 20px; }}
    .card {{ background: #fff; border-radius: 12px; max-width: 560px; margin: 0 auto; padding: 32px; box-shadow: 0 2px 12px rgba(0,0,0,0.08); }}
    .header {{ background: #042C53; border-radius: 8px; padding: 20px; text-align: center; margin-bottom: 24px; }}
    .header h1 {{ color: #fff; margin: 0; font-size: 22px; }}
    .header p {{ color: #B5D4F4; margin: 4px 0 0; font-size: 13px; }}
    .detail-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #E6F1FB; }}
    .detail-row:last-child {{ border-bottom: none; }}
    .label {{ color: #64748B; font-size: 13px; }}
    .value {{ color: #042C53; font-weight: 600; font-size: 13px; text-align: right; }}
    .badge {{ background: #DCFCE7; color: #15803D; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 600; display: inline-block; margin-top: 16px; }}
    .footer {{ text-align: center; color: #64748B; font-size: 12px; margin-top: 24px; }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='header'>
      <h1>&#x2705; Appointment Confirmed</h1>
      <p>MediBook — Your Health, Simplified</p>
    </div>
    <p style='color:#042C53;'>Hi <strong>{patientName}</strong>,</p>
    <p style='color:#64748B; font-size:14px;'>Your appointment has been successfully booked. Here are your details:</p>
    <div>
      <div class='detail-row'><span class='label'>Doctor</span><span class='value'>{doctorName}</span></div>
      <div class='detail-row'><span class='label'>Clinic</span><span class='value'>{clinicName}</span></div>
      <div class='detail-row'><span class='label'>Date</span><span class='value'>{dateText}</span></div>
      <div class='detail-row'><span class='label'>Time</span><span class='value'>{timeText}</span></div>
      <div class='detail-row'><span class='label'>Reason</span><span class='value'>{reason}</span></div>
      <div class='detail-row'><span class='label'>Consultation Fee</span><span class='value'>${fee:F2}</span></div>
    </div>
    <div class='badge'>&#x1F4C5; Appointment Confirmed</div>
    <div class='footer'>
      <p>Please arrive 10 minutes before your appointment time.</p>
      <p>Need to reschedule? Open the MediBook app to manage your appointments.</p>
      <p style='margin-top:16px;'>— The MediBook Team</p>
    </div>
  </div>
</body>
</html>";
    }

    public static string AppointmentReminder(string patientName, string doctorName, string timeText, string clinicName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: Arial, sans-serif; background: #F3F7FC; margin: 0; padding: 20px; }}
    .card {{ background: #fff; border-radius: 12px; max-width: 560px; margin: 0 auto; padding: 32px; box-shadow: 0 2px 12px rgba(0,0,0,0.08); }}
    .header {{ background: #C2740C; border-radius: 8px; padding: 20px; text-align: center; margin-bottom: 24px; }}
    .header h1 {{ color: #fff; margin: 0; font-size: 22px; }}
    .footer {{ text-align: center; color: #64748B; font-size: 12px; margin-top: 24px; }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='header'>
      <h1>&#x23F0; Appointment Reminder</h1>
    </div>
    <p style='color:#042C53;'>Hi <strong>{patientName}</strong>,</p>
    <p style='color:#64748B; font-size:14px;'>This is a reminder about your upcoming appointment <strong>today at {timeText}</strong> with <strong>{doctorName}</strong> at <strong>{clinicName}</strong>.</p>
    <p style='color:#64748B; font-size:14px;'>Please remember to bring any relevant medical records or test results.</p>
    <div class='footer'><p>— The MediBook Team</p></div>
  </div>
</body>
</html>";
    }

    public static string PasswordResetConfirmation(string userName)
    {
        return $@"<div style='font-family:Arial,sans-serif;max-width:500px;margin:0 auto;'>
            <h2 style='color:#042C53;'>Password Reset Requested</h2>
            <p>Hi {userName}, we received a request to reset your MediBook password.</p>
            <p>If you did not request this, please ignore this email and your password will remain unchanged.</p>
            <p style='color:#64748B;font-size:12px;'>— The MediBook Team</p>
        </div>";
    }

    public static string WelcomeEmail(string userName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif;background:#F3F7FC;margin:0;padding:20px;'>
  <div style='background:#fff;border-radius:12px;max-width:560px;margin:0 auto;padding:32px;'>
    <div style='background:#042C53;border-radius:8px;padding:20px;text-align:center;margin-bottom:24px;'>
      <h1 style='color:#fff;margin:0;'>Welcome to MediBook! &#x1F3E5;</h1>
    </div>
    <p style='color:#042C53;'>Hi <strong>{userName}</strong>,</p>
    <p style='color:#64748B;font-size:14px;'>Your account has been created successfully. You can now:</p>
    <ul style='color:#64748B;font-size:14px;'>
      <li>Book appointments with doctors</li>
      <li>Find clinics near you</li>
      <li>Manage your medical documents</li>
      <li>Set appointment reminders</li>
    </ul>
    <p style='color:#64748B;font-size:12px;margin-top:24px;'>— The MediBook Team</p>
  </div>
</body>
</html>";
    }
}
