﻿/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.IO;

namespace s2industries.ZUGFeRD
{
    internal class InvoiceDescriptor20Reader : IInvoiceDescriptorReader
    {
        public override InvoiceDescriptor Load(Stream stream)
        {
            if (!stream.CanRead)
            {
                throw new IllegalStreamException("Cannot read from stream");
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(stream);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.DocumentElement.OwnerDocument.NameTable);
            nsmgr.AddNamespace("qdt", "urn:un:unece:uncefact:data:standard:QualifiedDataType:10");
            nsmgr.AddNamespace("a", "urn:un:unece:uncefact:data:standard:QualifiedDataType:100");
            nsmgr.AddNamespace("rsm", "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100");
            nsmgr.AddNamespace("ram", "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100");
            nsmgr.AddNamespace("udt", "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100");

            InvoiceDescriptor retval = new InvoiceDescriptor
            {
                IsTest = _nodeAsBool(doc.DocumentElement, "//rsm:ExchangedDocumentContext/ram:TestIndicator/udt:Indicator", nsmgr),
                Profile = default(Profile).FromString(_nodeAsString(doc.DocumentElement, "//ram:GuidelineSpecifiedDocumentContextParameter/ram:ID", nsmgr)),
                Type = default(InvoiceType).FromString(_nodeAsString(doc.DocumentElement, "//rsm:ExchangedDocument/ram:TypeCode", nsmgr)),
                InvoiceNo = _nodeAsString(doc.DocumentElement, "//rsm:ExchangedDocument/ram:ID", nsmgr),
                InvoiceDate = _nodeAsDateTime(doc.DocumentElement, "//rsm:ExchangedDocument/ram:IssueDateTime/udt:DateTimeString", nsmgr)
            };

            foreach (XmlNode node in doc.SelectNodes("//rsm:ExchangedDocument/ram:IncludedNote", nsmgr))
            {
                string content = _nodeAsString(node, ".//ram:Content", nsmgr);
                string _subjectCode = _nodeAsString(node, ".//ram:SubjectCode", nsmgr);
                SubjectCodes subjectCode = default(SubjectCodes).FromString(_subjectCode);
                retval.AddNote(content, subjectCode);
            }

            retval.ReferenceOrderNo = _nodeAsString(doc, "//ram:ApplicableSupplyChainTradeAgreement/ram:BuyerReference", nsmgr);

            retval.Seller = _nodeAsParty(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeAgreement/ram:SellerTradeParty", nsmgr);
            foreach (XmlNode node in doc.SelectNodes("//ram:ApplicableSupplyChainTradeAgreement/ram:SellerTradeParty/ram:SpecifiedTaxRegistration", nsmgr))
            {
                string id = _nodeAsString(node, ".//ram:ID", nsmgr);
                string schemeID = _nodeAsString(node, ".//ram:ID/@schemeID", nsmgr);

                retval.AddSellerTaxRegistration(id, default(TaxRegistrationSchemeID).FromString(schemeID));
            }

            if (doc.SelectSingleNode("//ram:SellerTradeParty/ram:DefinedTradeContact", nsmgr) != null)
            {
                retval.SellerContact = new Contact()
                {
                    Name = _nodeAsString(doc.DocumentElement, "//ram:SellerTradeParty/ram:DefinedTradeContact/ram:PersonName", nsmgr),
                    OrgUnit = _nodeAsString(doc.DocumentElement, "//ram:SellerTradeParty/ram:DefinedTradeContact/ram:DepartmentName", nsmgr),
                    PhoneNo = _nodeAsString(doc.DocumentElement, "//ram:SellerTradeParty/ram:DefinedTradeContact/ram:TelephoneUniversalCommunication/ram:CompleteNumber", nsmgr),
                    FaxNo = _nodeAsString(doc.DocumentElement, "//ram:SellerTradeParty/ram:DefinedTradeContact/ram:FaxUniversalCommunication/ram:CompleteNumber", nsmgr),
                    EmailAddress = _nodeAsString(doc.DocumentElement, "//ram:SellerTradeParty/ram:DefinedTradeContact/ram:EmailURIUniversalCommunication/ram:CompleteNumber", nsmgr)
                };
            }

            retval.Buyer = _nodeAsParty(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeAgreement/ram:BuyerTradeParty", nsmgr);
            foreach (XmlNode node in doc.SelectNodes("//ram:ApplicableSupplyChainTradeAgreement/ram:BuyerTradeParty/ram:SpecifiedTaxRegistration", nsmgr))
            {
                string id = _nodeAsString(node, ".//ram:ID", nsmgr);
                string schemeID = _nodeAsString(node, ".//ram:ID/@schemeID", nsmgr);

                retval.AddBuyerTaxRegistration(id, default(TaxRegistrationSchemeID).FromString(schemeID));
            }

            if (doc.SelectSingleNode("//ram:BuyerTradeParty/ram:DefinedTradeContact", nsmgr) != null)
            {
                retval.BuyerContact = new Contact()
                {
                    Name = _nodeAsString(doc.DocumentElement, "//ram:BuyerTradeParty/ram:DefinedTradeContact/ram:PersonName", nsmgr),
                    OrgUnit = _nodeAsString(doc.DocumentElement, "//ram:BuyerTradeParty/DefinedTradeContact/ram:DepartmentName", nsmgr),
                    PhoneNo = _nodeAsString(doc.DocumentElement, "//ram:BuyerTradeParty/ram:DefinedTradeContact/ram:TelephoneUniversalCommunication/ram:CompleteNumber", nsmgr),
                    FaxNo = _nodeAsString(doc.DocumentElement, "//ram:BuyerTradeParty/ram:DefinedTradeContact/ram:FaxUniversalCommunication/ram:CompleteNumber", nsmgr),
                    EmailAddress = _nodeAsString(doc.DocumentElement, "//ram:BuyerTradeParty/ram:DefinedTradeContact/ram:EmailURIUniversalCommunication/ram:CompleteNumber", nsmgr)
                };
            }

            retval.ShipTo = _nodeAsParty(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeDelivery/ram:ShipToTradeParty", nsmgr);
            retval.ShipFrom = _nodeAsParty(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeDelivery/ram:ShipFromTradeParty", nsmgr);
            retval.ActualDeliveryDate = _nodeAsDateTime(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeDelivery/ram:ActualDeliverySupplyChainEvent/ram:OccurrenceDateTime/udt:DateTimeString", nsmgr);

            string _deliveryNoteNo = _nodeAsString(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeDelivery/ram:DeliveryNoteReferencedDocument/ram:ID", nsmgr);
            DateTime? _deliveryNoteDate = _nodeAsDateTime(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeDelivery/ram:DeliveryNoteReferencedDocument/ram:IssueDateTime/udt:DateTimeString", nsmgr);

            if (!_deliveryNoteDate.HasValue)
            {
                _deliveryNoteDate = _nodeAsDateTime(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeDelivery/ram:DeliveryNoteReferencedDocument/ram:IssueDateTime", nsmgr);
            }

            if (_deliveryNoteDate.HasValue || !String.IsNullOrEmpty(_deliveryNoteNo))
            {
                retval.DeliveryNoteReferencedDocument = new DeliveryNoteReferencedDocument()
                {
                    ID = _deliveryNoteNo,
                    IssueDateTime = _deliveryNoteDate
                };
            }

            retval.Invoicee = _nodeAsParty(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeSettlement/ram:InvoiceeTradeParty", nsmgr);
            retval.Payee = _nodeAsParty(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeSettlement/ram:PayeeTradeParty", nsmgr);

            retval.InvoiceNoAsReference = _nodeAsString(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeSettlement/ram:PaymentReference", nsmgr);
            retval.Currency = default(CurrencyCodes).FromString(_nodeAsString(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeSettlement/ram:InvoiceCurrencyCode", nsmgr));

            // TODO: Multiple SpecifiedTradeSettlementPaymentMeans can exist for each account/institution (with different SEPA?)
            PaymentMeans _tempPaymentMeans = new PaymentMeans()
            {
                TypeCode = default(PaymentMeansTypeCodes).FromString(_nodeAsString(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeSettlement/ram:SpecifiedTradeSettlementPaymentMeans/ram:TypeCode", nsmgr)),
                Information = _nodeAsString(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeSettlement/ram:SpecifiedTradeSettlementPaymentMeans/ram:Information", nsmgr),
                SEPACreditorIdentifier = _nodeAsString(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeSettlement/ram:SpecifiedTradeSettlementPaymentMeans/ram:ID", nsmgr),
                SEPAMandateReference = _nodeAsString(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeSettlement/ram:SpecifiedTradeSettlementPaymentMeans/ram:ID/@schemeAgencyID", nsmgr)
            };
            retval.PaymentMeans = _tempPaymentMeans;

            XmlNodeList creditorFinancialAccountNodes = doc.SelectNodes("//ram:ApplicableSupplyChainTradeSettlement/ram:SpecifiedTradeSettlementPaymentMeans/ram:PayeePartyCreditorFinancialAccount", nsmgr);
            XmlNodeList creditorFinancialInstitutions = doc.SelectNodes("//ram:ApplicableSupplyChainTradeSettlement/ram:SpecifiedTradeSettlementPaymentMeans/ram:PayeeSpecifiedCreditorFinancialInstitution", nsmgr);

            if (creditorFinancialAccountNodes.Count == creditorFinancialInstitutions.Count)
            {
                for (int i = 0; i < creditorFinancialAccountNodes.Count; i++)
                {
                    BankAccount _account = new BankAccount()
                    {
                        ID = _nodeAsString(creditorFinancialAccountNodes[0], ".//ram:ProprietaryID", nsmgr),
                        IBAN = _nodeAsString(creditorFinancialAccountNodes[0], ".//ram:IBANID", nsmgr),
                        BIC = _nodeAsString(creditorFinancialInstitutions[0], ".//ram:BICID", nsmgr),
                        Bankleitzahl = _nodeAsString(creditorFinancialInstitutions[0], ".//ram:GermanBankleitzahlID", nsmgr),
                        BankName = _nodeAsString(creditorFinancialInstitutions[0], ".//ram:Name", nsmgr),
                        Name = _nodeAsString(creditorFinancialInstitutions[0], ".//ram:AccountName", nsmgr),
                    };

                    retval.CreditorBankAccounts.Add(_account);
                } // !for(i)
            }

            XmlNodeList debitorFinancialAccountNodes = doc.SelectNodes("//ram:ApplicableSupplyChainTradeSettlement/ram:SpecifiedTradeSettlementPaymentMeans/ram:PayerPartyDebtorFinancialAccount", nsmgr);
            XmlNodeList debitorFinancialInstitutions = doc.SelectNodes("//ram:ApplicableSupplyChainTradeSettlement/ram:SpecifiedTradeSettlementPaymentMeans/ram:PayerSpecifiedDebtorFinancialInstitution", nsmgr);

            if (debitorFinancialAccountNodes.Count == debitorFinancialInstitutions.Count)
            {
                for (int i = 0; i < debitorFinancialAccountNodes.Count; i++)
                {
                    BankAccount _account = new BankAccount()
                    {
                        ID = _nodeAsString(debitorFinancialAccountNodes[0], ".//ram:ProprietaryID", nsmgr),
                        IBAN = _nodeAsString(debitorFinancialAccountNodes[0], ".//ram:IBANID", nsmgr),
                        BIC = _nodeAsString(debitorFinancialInstitutions[0], ".//ram:BICID", nsmgr),
                        Bankleitzahl = _nodeAsString(debitorFinancialInstitutions[0], ".//ram:GermanBankleitzahlID", nsmgr),
                        BankName = _nodeAsString(debitorFinancialInstitutions[0], ".//ram:Name", nsmgr),
                    };

                    retval.DebitorBankAccounts.Add(_account);
                } // !for(i)
            }

            foreach (XmlNode node in doc.SelectNodes("//ram:ApplicableSupplyChainTradeSettlement/ram:ApplicableTradeTax", nsmgr))
            {
                retval.AddApplicableTradeTax(_nodeAsDecimal(node, ".//ram:BasisAmount", nsmgr, 0).Value,
                                             _nodeAsDecimal(node, ".//ram:ApplicablePercent", nsmgr, 0).Value,
                                             default(TaxTypes).FromString(_nodeAsString(node, ".//ram:TypeCode", nsmgr)),
                                             default(TaxCategoryCodes).FromString(_nodeAsString(node, ".//ram:CategoryCode", nsmgr)));
            }

            foreach (XmlNode node in doc.SelectNodes("//ram:SpecifiedTradeAllowanceCharge", nsmgr))
            {
                retval.AddTradeAllowanceCharge(!_nodeAsBool(node, ".//ram:ChargeIndicator", nsmgr), // wichtig: das not (!) beachten
                                               _nodeAsDecimal(node, ".//ram:BasisAmount", nsmgr, 0).Value,
                                               retval.Currency,
                                               _nodeAsDecimal(node, ".//ram:ActualAmount", nsmgr, 0).Value,
                                               _nodeAsString(node, ".//ram:Reason", nsmgr),
                                               default(TaxTypes).FromString(_nodeAsString(node, ".//ram:CategoryTradeTax/ram:TypeCode", nsmgr)),
                                               default(TaxCategoryCodes).FromString(_nodeAsString(node, ".//ram:CategoryTradeTax/ram:CategoryCode", nsmgr)),
                                               _nodeAsDecimal(node, ".//ram:CategoryTradeTax/ram:ApplicablePercent", nsmgr, 0).Value);
            }

            foreach (XmlNode node in doc.SelectNodes("//ram:SpecifiedLogisticsServiceCharge", nsmgr))
            {
                retval.AddLogisticsServiceCharge(_nodeAsDecimal(node, ".//ram:AppliedAmount", nsmgr, 0).Value,
                                                 _nodeAsString(node, ".//ram:Description", nsmgr),
                                                 default(TaxTypes).FromString(_nodeAsString(node, ".//ram:AppliedTradeTax/ram:TypeCode", nsmgr)),
                                                 default(TaxCategoryCodes).FromString(_nodeAsString(node, ".//ram:AppliedTradeTax/ram:CategoryCode", nsmgr)),
                                                 _nodeAsDecimal(node, ".//ram:AppliedTradeTax/ram:ApplicablePercent", nsmgr, 0).Value);
            }

            retval.PaymentTerms = new PaymentTerms()
            {
                Description = _nodeAsString(doc.DocumentElement, "//ram:SpecifiedTradePaymentTerms/ram:Description", nsmgr),
                DueDate = _nodeAsDateTime(doc.DocumentElement, "//ram:SpecifiedTradePaymentTerms/ram:DueDateDateTime", nsmgr)
            };

            retval.LineTotalAmount = _nodeAsDecimal(doc.DocumentElement, "//ram:SpecifiedTradeSettlementHeaderMonetarySummation/ram:LineTotalAmount", nsmgr, 0).Value;
            retval.ChargeTotalAmount = _nodeAsDecimal(doc.DocumentElement, "//ram:SpecifiedTradeSettlementHeaderMonetarySummation/ram:ChargeTotalAmount", nsmgr, null);
            retval.AllowanceTotalAmount = _nodeAsDecimal(doc.DocumentElement, "//ram:SpecifiedTradeSettlementHeaderMonetarySummation/ram:AllowanceTotalAmount", nsmgr, null);
            retval.TaxBasisAmount = _nodeAsDecimal(doc.DocumentElement, "//ram:SpecifiedTradeSettlementHeaderMonetarySummation/ram:TaxBasisTotalAmount", nsmgr, null);
            retval.TaxTotalAmount = _nodeAsDecimal(doc.DocumentElement, "//ram:SpecifiedTradeSettlementHeaderMonetarySummation/ram:TaxTotalAmount", nsmgr, 0).Value;
            retval.GrandTotalAmount = _nodeAsDecimal(doc.DocumentElement, "//ram:SpecifiedTradeSettlementHeaderMonetarySummation/ram:GrandTotalAmount", nsmgr, 0).Value;
            retval.TotalPrepaidAmount = _nodeAsDecimal(doc.DocumentElement, "//ram:SpecifiedTradeSettlementHeaderMonetarySummation/ram:TotalPrepaidAmount", nsmgr, null);
            retval.DuePayableAmount = _nodeAsDecimal(doc.DocumentElement, "//ram:SpecifiedTradeSettlementHeaderMonetarySummation/ram:DuePayableAmount", nsmgr, 0).Value;

            retval.OrderDate = _nodeAsDateTime(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeAgreement/ram:BuyerOrderReferencedDocument/ram:IssueDateTime/udt:DateTimeString", nsmgr);
            if (!retval.OrderDate.HasValue)
            {
                retval.OrderDate = _nodeAsDateTime(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeAgreement/ram:BuyerOrderReferencedDocument/ram:IssueDateTime", nsmgr);
            }
            retval.OrderNo = _nodeAsString(doc.DocumentElement, "//ram:ApplicableSupplyChainTradeAgreement/ram:BuyerOrderReferencedDocument/ram:ID", nsmgr);


            foreach (XmlNode node in doc.SelectNodes("//ram:IncludedSupplyChainTradeLineItem", nsmgr))
            {
                retval.TradeLineItems.Add(_parseTradeLineItem(node, nsmgr));
            }
            return retval;
        } // !Load()        


        public override bool IsReadableByThisReaderVersion(Stream stream)
        {
            List<string> validURIs = new List<string>()
                {
                    "urn:ferd:invoice:rc:basic",
                    "urn:ferd:CrossIndustryDocument:invoice:1p0:basic",
                    "urn:ferd:invoice:rc:comfort",
                    "urn:ferd:CrossIndustryDocument:invoice:1p0:comfort",
                    "urn:ferd:CrossIndustryDocument:invoice:1p0:E",
                    "urn:ferd:invoice:rc:extended"                    
                };

            long _oldStreamPosition = stream.Position;
            stream.Position = 0;
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
            {
                string data = reader.ReadToEnd();               
                foreach(string validURI in validURIs)
                {
                    if (data.Contains(validURI))
                    {
                        stream.Position = _oldStreamPosition;
                        return true;
                    }
                }
            }

            stream.Position = _oldStreamPosition;
            return false;
        } // !IsReadableByThisReaderVersion()
        

        private static TradeLineItem _parseTradeLineItem(XmlNode tradeLineItem, XmlNamespaceManager nsmgr = null)
        {
            if (tradeLineItem == null)
            {
                return null;
            }

            TradeLineItem item = new TradeLineItem()
            {
                GlobalID = new GlobalID(_nodeAsString(tradeLineItem, ".//ram:SpecifiedTradeProduct/ram:GlobalID/@schemeID", nsmgr),
                                        _nodeAsString(tradeLineItem, ".//ram:SpecifiedTradeProduct/ram:GlobalID", nsmgr)),
                SellerAssignedID = _nodeAsString(tradeLineItem, ".//ram:SpecifiedTradeProduct/ram:SellerAssignedID", nsmgr),
                BuyerAssignedID = _nodeAsString(tradeLineItem, ".//ram:SpecifiedTradeProduct/ram:BuyerAssignedID", nsmgr),
                Name = _nodeAsString(tradeLineItem, ".//ram:SpecifiedTradeProduct/ram:Name", nsmgr),
                Description = _nodeAsString(tradeLineItem, ".//ram:SpecifiedTradeProduct/ram:Description", nsmgr),
                UnitQuantity = _nodeAsDecimal(tradeLineItem, ".//ram:BasisQuantity", nsmgr, 1),
                BilledQuantity = _nodeAsDecimal(tradeLineItem, ".//ram:BilledQuantity", nsmgr, 0).Value,
                LineTotalAmount = _nodeAsDecimal(tradeLineItem, ".//ram:LineTotalAmount", nsmgr, 0),
                TaxCategoryCode = default(TaxCategoryCodes).FromString(_nodeAsString(tradeLineItem, ".//ram:ApplicableTradeTax/ram:CategoryCode", nsmgr)),
                TaxType = default(TaxTypes).FromString(_nodeAsString(tradeLineItem, ".//ram:ApplicableTradeTax/ram:TypeCode", nsmgr)),
                TaxPercent = _nodeAsDecimal(tradeLineItem, ".//ram:ApplicableTradeTax/ram:ApplicablePercent", nsmgr, 0).Value,
                NetUnitPrice = _nodeAsDecimal(tradeLineItem, ".//ram:NetPriceProductTradePrice/ram:ChargeAmount", nsmgr, 0).Value,
                GrossUnitPrice = _nodeAsDecimal(tradeLineItem, ".//ram:GrossPriceProductTradePrice/ram:ChargeAmount", nsmgr, 0).Value,
                UnitCode = default(QuantityCodes).FromString(_nodeAsString(tradeLineItem, ".//ram:BasisQuantity/@unitCode", nsmgr))
            };

            if (tradeLineItem.SelectSingleNode(".//ram:AssociatedDocumentLineDocument", nsmgr) != null)
            {
                item.AssociatedDocument = new AssociatedDocument()
                {
                    LineID = _nodeAsInt(tradeLineItem, ".//ram:AssociatedDocumentLineDocument/ram:LineID", nsmgr, Int32.MaxValue)
                };

                XmlNodeList noteNodes = tradeLineItem.SelectNodes(".//ram:AssociatedDocumentLineDocument/ram:IncludedNote", nsmgr);
                foreach (XmlNode noteNode in noteNodes)
                {
                    item.AssociatedDocument.Notes.Add(new Note(
                                content: _nodeAsString(noteNode, ".//ram:Content", nsmgr),
                                subjectCode: default(SubjectCodes).FromString(_nodeAsString(noteNode, ".//ram:SubjectCode", nsmgr)),
                                contentCode: ContentCodes.Unknown
                    ));
                }

                if (item.AssociatedDocument.LineID == Int32.MaxValue) // a bit dirty, but works for now
                {
                    item.AssociatedDocument.LineID = null;
                }
            }

            XmlNodeList appliedTradeAllowanceChargeNodes = tradeLineItem.SelectNodes(".//ram:SpecifiedSupplyChainTradeAgreement/ram:GrossPriceProductTradePrice/ram:AppliedTradeAllowanceCharge", nsmgr);
            foreach (XmlNode appliedTradeAllowanceChargeNode in appliedTradeAllowanceChargeNodes)
            {
                bool chargeIndicator = _nodeAsBool(appliedTradeAllowanceChargeNode, "./ram:ChargeIndicator/udt:Indicator", nsmgr);
                decimal basisAmount = _nodeAsDecimal(appliedTradeAllowanceChargeNode, "./ram:BasisAmount", nsmgr, 0).Value;
                string basisAmountCurrency = _nodeAsString(appliedTradeAllowanceChargeNode, "./ram:BasisAmount/@currencyID", nsmgr);
                decimal actualAmount = _nodeAsDecimal(appliedTradeAllowanceChargeNode, "./ram:ActualAmount", nsmgr, 0).Value;
                string actualAmountCurrency = _nodeAsString(appliedTradeAllowanceChargeNode, "./ram:ActualAmount/@currencyID", nsmgr);
                string reason = _nodeAsString(appliedTradeAllowanceChargeNode, "./ram:Reason", nsmgr);

                item.addTradeAllowanceCharge(!chargeIndicator, // wichtig: das not (!) beachten
                                                default(CurrencyCodes).FromString(basisAmountCurrency),
                                                basisAmount,
                                                actualAmount,
                                                reason);
            }

            if (item.UnitCode == QuantityCodes.Unknown)
            {
                // UnitCode alternativ aus BilledQuantity extrahieren
                item.UnitCode = default(QuantityCodes).FromString(_nodeAsString(tradeLineItem, ".//ram:BilledQuantity/@unitCode", nsmgr));
            }

            if (tradeLineItem.SelectSingleNode(".//ram:SpecifiedSupplyChainTradeAgreement/ram:BuyerOrderReferencedDocument/ram:ID", nsmgr) != null)
            {
                item.BuyerOrderReferencedDocument = new BuyerOrderReferencedDocument()
                {
                    ID = _nodeAsString(tradeLineItem, ".//ram:SpecifiedSupplyChainTradeAgreement/ram:BuyerOrderReferencedDocument/ram:ID", nsmgr),
                    IssueDateTime = _nodeAsDateTime(tradeLineItem, ".//ram:SpecifiedSupplyChainTradeAgreement/ram:BuyerOrderReferencedDocument/ram:IssueDateTime", nsmgr),
                };
            }

            if (tradeLineItem.SelectSingleNode(".//ram:SpecifiedSupplyChainTradeDelivery/ram:DeliveryNoteReferencedDocument/ram:ID", nsmgr) != null)
            {
                item.DeliveryNoteReferencedDocument = new DeliveryNoteReferencedDocument()
                {
                    ID = _nodeAsString(tradeLineItem, ".//ram:SpecifiedSupplyChainTradeDelivery/ram:DeliveryNoteReferencedDocument/ram:ID", nsmgr),
                    IssueDateTime = _nodeAsDateTime(tradeLineItem, ".//ram:SpecifiedSupplyChainTradeDelivery/ram:DeliveryNoteReferencedDocument/ram:IssueDateTime", nsmgr),
                };
            }

            if (tradeLineItem.SelectSingleNode(".//ram:SpecifiedSupplyChainTradeDelivery/ram:ActualDeliverySupplyChainEvent/ram:OccurrenceDateTime", nsmgr) != null)
            {
                item.ActualDeliveryDate = _nodeAsDateTime(tradeLineItem, ".//ram:SpecifiedSupplyChainTradeDelivery/ram:ActualDeliverySupplyChainEvent/ram:OccurrenceDateTime/udt:DateTimeString", nsmgr);
            }

            if (tradeLineItem.SelectSingleNode(".//ram:SpecifiedSupplyChainTradeAgreement/ram:ContractReferencedDocument/ram:ID", nsmgr) != null)
            {
                item.ContractReferencedDocument = new ContractReferencedDocument()
                {
                    ID = _nodeAsString(tradeLineItem, ".//ram:SpecifiedSupplyChainTradeAgreement/ram:ContractReferencedDocument/ram:ID", nsmgr),
                    IssueDateTime = _nodeAsDateTime(tradeLineItem, ".//ram:SpecifiedSupplyChainTradeAgreement/ram:ContractReferencedDocument/ram:IssueDateTime", nsmgr),
                };
            }

            XmlNodeList referenceNodes = tradeLineItem.SelectNodes(".//ram:SpecifiedSupplyChainTradeAgreement/ram:AdditionalReferencedDocument", nsmgr);
            foreach (XmlNode referenceNode in referenceNodes)
            {
                string _code = _nodeAsString(referenceNode, "ram:ReferenceTypeCode", nsmgr);

                item.addAdditionalReferencedDocument(
                    id: _nodeAsString(referenceNode, "ram:ID", nsmgr),
                    date: _nodeAsDateTime(referenceNode, "ram:IssueDateTim", nsmgr),
                    code: default(ReferenceTypeCodes).FromString(_code)
                );
            }

            if (tradeLineItem.SelectSingleNode(".//ram:SpecifiedSupplyChainTradeAgreement/ram:ContractReferencedDocument/ram:ID", nsmgr) != null)
            {
                item.ContractReferencedDocument = new ContractReferencedDocument()
                {
                    ID = _nodeAsString(tradeLineItem, ".//ram:SpecifiedSupplyChainTradeAgreement/ram:ContractReferencedDocument/ram:ID", nsmgr),
                    IssueDateTime = _nodeAsDateTime(tradeLineItem, ".//ram:SpecifiedSupplyChainTradeAgreement/ram:ContractReferencedDocument/ram:IssueDateTime", nsmgr),
                };
            }

            return item;
        } // !_parseTradeLineItem()


        private static Party _nodeAsParty(XmlNode baseNode, string xpath, XmlNamespaceManager nsmgr = null)
        {
            if (baseNode == null)
            {
                return null;
            }

            XmlNode node = baseNode.SelectSingleNode(xpath, nsmgr);
            if (node == null)
            {
                return null;
            }

            Party retval = new Party()
            {
                ID = _nodeAsString(node, "ram:ID", nsmgr),
                GlobalID = new GlobalID(_nodeAsString(node, "ram:GlobalID/@schemeID", nsmgr),
                                        _nodeAsString(node, "ram:GlobalID", nsmgr)),
                Name = _nodeAsString(node, "ram:Name", nsmgr),
                Postcode = _nodeAsString(node, "ram:PostalTradeAddress/ram:PostcodeCode", nsmgr),
                City = _nodeAsString(node, "ram:PostalTradeAddress/ram:CityName", nsmgr),
                Country = default(CountryCodes).FromString(_nodeAsString(node, "ram:PostalTradeAddress/ram:CountryID", nsmgr))
            };

            string lineOne = _nodeAsString(node, "ram:PostalTradeAddress/ram:LineOne", nsmgr);
            string lineTwo = _nodeAsString(node, "ram:PostalTradeAddress/ram:LineTwo", nsmgr);

            if (!String.IsNullOrEmpty(lineTwo))
            {
                retval.ContactName = lineOne;
                retval.Street = lineOne;
            }
            else
            {
                retval.Street = lineOne;
                retval.ContactName = null;
            }

            return retval;
        } // !_nodeAsParty()
    }
}
