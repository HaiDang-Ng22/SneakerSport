	CREATE TABLE Users (
    Username NVARCHAR(255) NOT NULL,
    Password NCHAR(50) NOT NULL,
    UserRole NVARCHAR(MAX) NOT NULL,
    PRIMARY KEY CLUSTERED (Username ASC)
);
select*from Users