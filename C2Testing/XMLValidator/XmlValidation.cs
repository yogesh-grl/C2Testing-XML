using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.XmlDiffPatch;
using System.Xml.Schema;

namespace C2Testing.XMLValidator
{
    public class XmlValidation
    {
        public string GoldenXMLPath;
        public string CurrentXMLPath;

        public static string TestNode = "test";
        public static string ScoreNode = "score";
        public static string ConditionNode = "condition";
        public static string ConditionsNode = "conditions";
        public static string checkNode = "check";
        public static string checksNode = "checks";

        public static string GoldenReportValue = "Golden Report";
        public static string CurrentReportValue = "Current Report";

        public XmlValidation(string goldenXMLPath, string currentXMLPath)
        {
            GoldenXMLPath = goldenXMLPath;
            CurrentXMLPath = currentXMLPath;
        }

        List<XElement> SimpleStreamAxis(string inputUrl)
        {
            List<XElement> lsElements = new List<XElement>();
            using (XmlReader reader = XmlReader.Create(inputUrl))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == TestNode)
                        {
                            XElement el = XNode.ReadFrom(reader) as XElement;
                            if (el != null)
                            {
                                lsElements.Add(el);
                            }
                        }
                    }
                }
            }
            return lsElements;
        }

        public bool ValidateXML()
        {
            bool retVal = true;
            try
            {
                //XmlDiff xmldiff = new XmlDiff(XmlDiffOptions.IgnoreComments |
                //                  XmlDiffOptions.IgnoreNamespaces |
                //                  XmlDiffOptions.IgnorePrefixes | XmlDiffOptions.IgnoreWhitespace);
                //XmlTextWriter diffgram = new XmlTextWriter(Console.Out);
                //bool bIdentical = xmldiff.Compare(GoldenXMLPath, CurrentXMLPath, false, diffgram);

                List<XElement> ListGoldenTestNodes = SimpleStreamAxis(GoldenXMLPath);
                List<XElement> ListCurrentTestNodes = SimpleStreamAxis(CurrentXMLPath);

                DeepCompare(ListGoldenTestNodes, ListCurrentTestNodes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{System.Reflection.MethodInfo.GetCurrentMethod().Name} - {ex}");
                retVal = false;
            }
            return retVal;
        }

        private static void DeepCompare(List<XElement> ListGoldenTestNodes, List<XElement> ListCurrentTestNodes)
        {
            try
            {
                foreach (XElement currentEle in ListCurrentTestNodes)
                {
                    //check the current node (current/each  test case detail) in the {GoldenReportValue}
                    // if mean proceed else add to the error case 
                    XElement goldenEle = ListGoldenTestNodes.Find(x => x.FirstAttribute.Value == currentEle.FirstAttribute.Value);
                    if (goldenEle != null)
                    {
                        bool deepCompare = XNode.DeepEquals(goldenEle, currentEle);
                        if (deepCompare == false)
                        {
                            Console.WriteLine($"{currentEle.FirstAttribute.Value} - MisMatch Found");
                            //If mismatch found - check in detail all nodes 

                            //get the current test case equivalent from the {GoldenReportValue}
                            foreach (var GoldenEle in ListGoldenTestNodes)
                            {
                                //Overall Test Score - Compare
                                CompareScoreElement(currentEle, GoldenEle, ScoreNode);

                                //Conditions 
                                VerifyConditionsElement(currentEle, goldenEle);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{currentEle.FirstAttribute.Value} - Expected");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Missing {currentEle.FirstAttribute.Value} in the {GoldenReportValue}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{System.Reflection.MethodInfo.GetCurrentMethod().Name} - {ex}");
            }
        }

        private static void VerifyConditionsElement(XElement currentEle, XElement goldenEle)
        {
            List<XElement> elConditionCurrentReport, elConditionGoldenReport;
            GetXMLElements(currentEle, goldenEle, "conditions", out elConditionCurrentReport, out elConditionGoldenReport);
            if (elConditionGoldenReport != null)
            {
                if (elConditionCurrentReport != null)
                {
                    if (elConditionCurrentReport.Count == elConditionGoldenReport.Count)
                    {
                        //Condition
                        List<XElement> elActualConditionCurrentReport = null;
                        List<XElement> elActualConditionGoldenReport = null;
                        GetXMLElementsFromElementList(elConditionCurrentReport, elConditionGoldenReport, "condition", out elActualConditionCurrentReport, out elActualConditionGoldenReport);
                        VerifyAllContionElements(elActualConditionCurrentReport, elActualConditionGoldenReport);
                    }
                    else
                    {
                        Console.WriteLine($"Mimatch conditions count :" +
                            $" {GoldenReportValue} {elConditionGoldenReport.Count} - {CurrentReportValue} {elConditionCurrentReport.Count}");
                    }
                }
                else
                {
                    Console.WriteLine($"conditions missing in the {CurrentReportValue}");
                }
            }
            else
            {
                Console.WriteLine($"conditions missing in the {GoldenReportValue}");
            }
        }

        private static void VerifyAllContionElements(List<XElement> elActualConditionCurrentReport, List<XElement> elActualConditionGoldenReport)
        {
            if (elActualConditionGoldenReport != null)
            {
                if (elActualConditionCurrentReport != null)
                {
                    if (elActualConditionCurrentReport.Count == elActualConditionGoldenReport.Count)
                    {
                        //TODO :: Check the DESC check using the Deep check 
                        for (int x = 0; x < elActualConditionCurrentReport.Count; x++)
                        {
                            if (XNode.DeepEquals(elActualConditionCurrentReport[x], elActualConditionGoldenReport[x]) == false)
                            {
                                //Check the Condition -> Score 
                                CompareScoreElement(elActualConditionCurrentReport[x], elActualConditionGoldenReport[x], ScoreNode);
                                VerifyOverallChecks(elActualConditionCurrentReport, elActualConditionGoldenReport);
                            }
                            else
                            {
                                // All the test conditions are proper 
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"condition missing in the {CurrentReportValue}");
                }
            }
            else
            {
                Console.WriteLine($"condition missing in the {GoldenReportValue}");
            }
        }

        private static void VerifyOverallChecks(List<XElement> elActualConditionCurrentReport, List<XElement> elActualConditionGoldenReport)
        {
            List<XElement> elActualOverallCheckCurrentReport = null; List<XElement> elActualOverallChecksGoldenReport = null;
            GetXMLElementsFromElementList(elActualConditionCurrentReport, elActualConditionGoldenReport, "checks", out elActualOverallCheckCurrentReport, out elActualOverallChecksGoldenReport);
            if (elActualOverallChecksGoldenReport != null)
            {
                if (elActualOverallCheckCurrentReport != null)
                {
                    if (elActualOverallChecksGoldenReport.Count != elActualOverallCheckCurrentReport.Count)
                    {
                        Console.WriteLine($"Mimatch checks count :" + $" {GoldenReportValue} {elActualOverallChecksGoldenReport.Count} - {CurrentReportValue} {elActualOverallCheckCurrentReport.Count}");
                    }
                    else
                    {
                        // proper // check in Deep 
                        VerifyChecks(elActualOverallCheckCurrentReport, elActualOverallChecksGoldenReport);

                    }
                }
                else
                {
                    Console.WriteLine($"checks missing in the {CurrentReportValue}");

                }
            }
            else
            {
                Console.WriteLine($"checks missing in the {GoldenReportValue}");

            }
        }

        private static void VerifyChecks(List<XElement> elActualOverallCheckCurrentReport, List<XElement> elActualOverallChecksGoldenReport)
        {
            List<XElement> elActualCheckCurrentReport = null;
            List<XElement> elActualChecksGoldenReport = null;
            GetXMLElementsFromElementList(elActualOverallCheckCurrentReport, elActualOverallChecksGoldenReport, "check", out elActualCheckCurrentReport, out elActualChecksGoldenReport);
            if (elActualChecksGoldenReport != null)
            {
                if (elActualCheckCurrentReport != null)
                {
                    if (elActualChecksGoldenReport.Count == elActualCheckCurrentReport.Count)
                    {
                        for (int z = 0; z < elActualChecksGoldenReport.Count; z++)
                        {
                            if (XNode.DeepEquals(elActualChecksGoldenReport[z], elActualCheckCurrentReport[z]) == false)
                            {
                                //iterate all the check and perform the deep check - if mismatch then take the next attrbute and verify
                                for (int i = 0; i < elActualChecksGoldenReport.Count; i++)
                                {

                                }
                            }
                            else
                            {
                                // same 
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Mimatch check count :" + $" {GoldenReportValue} {elActualChecksGoldenReport.Count} - {CurrentReportValue} {elActualOverallCheckCurrentReport.Count}");
                    }
                }
                else
                {
                    Console.WriteLine($"check missing in the {GoldenReportValue}");
                }
            }
            else
            {
                Console.WriteLine($"check missing in the {GoldenReportValue}");
            }
        }

        private static void GetXMLElements(XElement currentEle, XElement goldenEle, string ElementName, out List<XElement> elCurrentReport, out List<XElement> elGoldenReport)
        {
            elCurrentReport = null;
            elGoldenReport = null;
            try
            {
                elCurrentReport = currentEle.Elements().Where(x => x.NodeType == XmlNodeType.Element && x.Name == ElementName).ToList();
                elGoldenReport = goldenEle.Elements().Where(x => x.NodeType == XmlNodeType.Element && x.Name == ElementName).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{System.Reflection.MethodInfo.GetCurrentMethod().Name} - {ex}");
            }
        }

        private static void GetXMLElementsFromElementList(List<XElement> currentEle, List<XElement> goldenEle, string ElementName, out List<XElement> elCurrentReport, out List<XElement> elGoldenReport)
        {
            elCurrentReport = null;
            elGoldenReport = null;
            try
            {
                elCurrentReport = currentEle.Elements().Where(x => x.NodeType == XmlNodeType.Element && x.Name == ElementName).ToList();
                elGoldenReport = goldenEle.Elements().Where(x => x.NodeType == XmlNodeType.Element && x.Name == ElementName).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{System.Reflection.MethodInfo.GetCurrentMethod().Name} - {ex}");
            }
        }

        private static bool CompareScoreElement(XElement currentReportElement, XElement GoldenReportElement, string ElementName = "")
        {
            bool retVal = true;
            List<XElement> elScoreGoldenReport = GoldenReportElement.Elements().Where(x => x.NodeType == XmlNodeType.Element && x.Name == ElementName).ToList();
            if (elScoreGoldenReport != null)
            {
                List<XElement> elScoreCurrentReport = currentReportElement.Elements().Where(x => x.NodeType == XmlNodeType.Element && x.Name == ElementName).ToList();
                if (elScoreCurrentReport != null)
                {
                    if (elScoreGoldenReport.Count != elScoreCurrentReport.Count)
                    {
                        Console.WriteLine($"Mismatch in the TestResult");
                        retVal = false;
                    }
                    else
                    {
                        retVal = ElementCompare(elScoreGoldenReport, elScoreCurrentReport);
                    }
                }
            }
            return retVal;
        }

        private static bool ElementCompare(List<XElement> GoldenReportElement, List<XElement> CurrentReportElement)
        {
            bool retVal = true;
            string goldenReportFirstAtt = "";
            string currentReportFirstAtt = "";
            for (int x = 0, y = 0; x < CurrentReportElement.Count && y < CurrentReportElement.Count; x++, y++)
            {
                if (XNode.DeepEquals(CurrentReportElement[x], GoldenReportElement[y]) == false)
                {
                    if (GoldenReportElement[x].FirstAttribute != null)
                    {
                        goldenReportFirstAtt = GoldenReportElement[x].FirstAttribute.Value;

                        if (CurrentReportElement[x].FirstAttribute != null)
                        {
                            currentReportFirstAtt = CurrentReportElement[x].FirstAttribute.Value;

                            string ParentAttrValue = "";
                            if (CurrentReportElement[x].Parent != null)
                            {
                                ParentAttrValue = CurrentReportElement[x].Parent.FirstAttribute.Value;
                            }

                            Console.WriteLine($"{ParentAttrValue} {GoldenReportValue} {GoldenReportElement[x].Name} : {goldenReportFirstAtt} " +
                        $"{CurrentReportValue} {CurrentReportElement[x].Name} : {goldenReportFirstAtt} ");
                            retVal = false;

                        }
                        else
                        {
                            Console.WriteLine("Unexpected Deep Check - Check this case manually");
                            retVal = false;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unexpected Deep Check - Check this case manually");
                        retVal = false;
                    }
                }
                else
                {
                    //same 
                }
            }
            return retVal;
        }
    }
}
