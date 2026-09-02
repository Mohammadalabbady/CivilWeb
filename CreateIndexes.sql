-- Create indexes for faster name search
CREATE NONCLUSTERED INDEX IX_CivilData_FirstName ON CivilData(FirstName);
CREATE NONCLUSTERED INDEX IX_CivilData_SecondName ON CivilData(SecondName);
CREATE NONCLUSTERED INDEX IX_CivilData_ThirdName ON CivilData(ThirdName);
CREATE NONCLUSTERED INDEX IX_CivilData_LastName ON CivilData(LastName);
CREATE NONCLUSTERED INDEX IX_CivilData_FullName ON CivilData(FullName);
CREATE NONCLUSTERED INDEX IX_CivilData_RegistrationNumber ON CivilData(RegistrationNumber);
