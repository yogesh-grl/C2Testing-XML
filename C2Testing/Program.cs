using C2Testing.XMLValidator;

Console.WriteLine("Enter the Golden Report XML file Path");
string goldenReportPath = @"C:\\GRL\\USBPD-C2-Browser-App\\Report\\TempReport\\AA-I6133 - Debug with 3s delay Rerun_2022_11_14-15_18_44\\New_Run1_Rep0_2022_11_14-03_18_44\\PDMerged\\GRLReport.xml";//Console.ReadLine();
Console.WriteLine("Enter the {CurrentReportValue} XML file Path");
string currentReportPath = @"C:\\GRL\\USBPD-C2-Browser-App\\Report\\TempReport\\AA-I6133 - Debug with 3s delay Rerun_2022_11_14-15_18_44\\New_Run1_Rep0_2022_11_14-03_18_44\\PDMerged\\Error.xml";//Console.ReadLine();

XmlValidation objXMLValidation = new XmlValidation(goldenReportPath, currentReportPath);
objXMLValidation.ValidateXML();

