SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Earlier local builds persisted a truncated raw User-Agent in DeviceLabel.
-- Remove that fingerprinting data; new writes use SessionDeviceLabel.Sanitize.
UPDATE [identity].[Session]
SET [DeviceLabel] = N'Browser session'
WHERE [DeviceLabel] <> N'Browser session';

COMMIT TRANSACTION;
