-- 检查并重命名 Room -> Rooms
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Houses' AND COLUMN_NAME = 'Room')
BEGIN
    EXEC sp_rename 'Houses.Room', 'Rooms', 'COLUMN';
    PRINT 'Room -> Rooms 重命名完成';
END

-- 检查并重命名 Bathroom -> Bathrooms
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Houses' AND COLUMN_NAME = 'Bathroom')
BEGIN
    EXEC sp_rename 'Houses.Bathroom', 'Bathrooms', 'COLUMN';
    PRINT 'Bathroom -> Bathrooms 重命名完成';
END

-- 检查并重命名 Image -> ImageUrl
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Houses' AND COLUMN_NAME = 'Image')
BEGIN
    EXEC sp_rename 'Houses.Image', 'ImageUrl', 'COLUMN';
    PRINT 'Image -> ImageUrl 重命名完成';
END
