-- Add Google reCAPTCHA v2 keys to SystemParameters
INSERT IGNORE INTO SystemParameters (`Key`, `Value`, `Description`, `CreatedAt`)
VALUES 
    ('CAPTCHA_SITE_KEY', '6LeIxAcTAAAAAJcZVRqyHh71UMIEGNQ_MXjiZKhI', 'Google reCAPTCHA v2 Site Key (Default is Google Test Key)', UTC_TIMESTAMP()),
    ('CAPTCHA_SECRET_KEY', '6LeIxAcTAAAAAGG-vFI1TnRWxMZNFuojJ4WifJWe', 'Google reCAPTCHA v2 Secret Key (Default is Google Test Key)', UTC_TIMESTAMP());
