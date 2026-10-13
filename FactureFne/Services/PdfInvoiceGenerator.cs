using FactureFne.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Globalization;


namespace FactureFne.Services
{
    internal class PdfInvoiceGenerator
    {
        CultureInfo fr = new CultureInfo("fr-FR");
        public void GenerateInvoicePdf(
            CompanyInfo Entreprise,
            InvoiceInfo invoice,
            ClientInfo client,
            List<InvoiceItem> items,
            List<CustomTax> customTaxes,
            List<GlobalCustomTax> globalCustomTaxes,
            string filePath)
        {
            Document doc = new Document(PageSize.A4, 50, 50, 40, 50);
            PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
            doc.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
            var subFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLACK);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, BaseColor.BLACK);

            // --- Logo ---
            string logoPath = Properties.Settings.Default.LogoPath; // chemin sauvegardé
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(logoPath);
                logo.ScaleAbsolute(80, 80);         // taille du logo
                logo.Alignment = Element.ALIGN_LEFT;
                doc.Add(logo);
            }

            // --- En-tête facture / avoir ---
            PdfPTable headerTable = new PdfPTable(1);
            headerTable.WidthPercentage = 105;

            // Cellule à droite sans bordure
            PdfPCell cell;

            // Si c'est une facture d'avoir
            if (invoice.TypeFacture != null && invoice.TypeFacture.ToUpper() == "AVOIR")
            {
                // AVOIR numéro
                cell = new PdfPCell(new Phrase($"AVOIR N° {invoice.NumeroFacture}", titleFont));
                cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                cell.Border = Rectangle.NO_BORDER;
                headerTable.AddCell(cell);

                // Facture d'origine
                if (!string.IsNullOrEmpty(invoice.ParentReference))
                {
                    cell = new PdfPCell(new Phrase($"Facture d'origine : {invoice.ParentReference}", subFont));
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.Border = Rectangle.NO_BORDER;
                    headerTable.AddCell(cell);
                }
            }
            else if (invoice.TypeFacture != null && invoice.TypeFacture.ToUpper() == "DEVIS") 
            {
                // Facture PROFORMA
                cell = new PdfPCell(new Phrase($"DEVIS N° {invoice.NumeroFacture}", titleFont));
                cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                cell.Border = Rectangle.NO_BORDER;
                headerTable.AddCell(cell);
            }
            else
            {
                // Facture classique
                cell = new PdfPCell(new Phrase($"FACTURE N° {invoice.NumeroFacture}", titleFont));
                cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                cell.Border = Rectangle.NO_BORDER;
                headerTable.AddCell(cell);
            }

           
            // Espacement sous le bloc
            headerTable.SpacingAfter = 18;

            // Ajout au document
            doc.Add(headerTable);


            // --- Informations société / client ---
            PdfPTable infoTable = new PdfPTable(2);
            infoTable.WidthPercentage = 105;
            infoTable.SetWidths(new float[] { 2f, 1f });

            PdfPCell sellerCell = new PdfPCell(new Phrase(
                $"Émetteur : {Entreprise?.Establishment ?? ""}\n" +
                $"\nPoint de Vente : {Entreprise?.PointOfSale ?? ""}\n" +
                $"\nNCC : {Entreprise?.NccEntreprise ?? ""}\n" +
                $"\nVendeur : {Entreprise?.ClientSellerName ?? ""}\n" +
                $"\nDate : {invoice.DateFacture:dd/MM/yyyy}\n",
                normalFont));
            sellerCell.Border = PdfPCell.NO_BORDER;
            infoTable.AddCell(sellerCell);

            PdfPCell clientCell = new PdfPCell(new Phrase(
                $"\nClient : {client?.Libelle ?? ""}\n" +
                $"\nNcc : {client?.Cc ?? ""}\n" +
                $"\nTéléphone : {client?.Tel ?? ""}\n" +
                $"\nEmail : {client?.Email ?? ""}\n",
                normalFont));
            clientCell.Border = PdfPCell.NO_BORDER;
            infoTable.AddCell(clientCell);

            doc.Add(infoTable);

            doc.Add(new Paragraph(" ", new Font(Font.FontFamily.HELVETICA, 12f)) { SpacingBefore = 5f });

            doc.Add(new Paragraph($"\n{Entreprise.CommercialMessage}\n", normalFont) { SpacingAfter = 20f });

            // --- Tableau des articles ---
            PdfPTable table = new PdfPTable(6);
            table.WidthPercentage = 105;
            table.SetWidths(new float[] { 1, 4, 1, 2, 1, 2 });

            string[] headers = { "Réf", "Désignation", "Qté", "Prix U", "Remise(%)", "Montant HT" };
            foreach (var h in headers)
            {
                PdfPCell headerCell = new PdfPCell(new Phrase(h, subFont));
                headerCell.BackgroundColor = BaseColor.LIGHT_GRAY;
                headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                table.AddCell(headerCell);
            }

            decimal totalHtAvantRemise = 0m;

            // --- Calcul HT total ---

            foreach (var item in items)
            {
                decimal lineBrut = item.Quantite * item.PrixUnitaire;
                decimal lineRemise = lineBrut * (item.Remise / 100m);
                decimal lineNet = lineBrut - lineRemise;

                totalHtAvantRemise += lineNet;

                // Cellules texte normales (alignées à gauche par défaut)
                table.AddCell(new PdfPCell(new Phrase(item.CodeArticle ?? "", normalFont)));
                table.AddCell(new PdfPCell(new Phrase(item.Libelle ?? "", normalFont)));

                // Cellules numériques alignées à droite
                PdfPCell cellQuantite = new PdfPCell(new Phrase(item.Quantite.ToString("N0", fr), normalFont));
                cellQuantite.HorizontalAlignment = Element.ALIGN_RIGHT;
                table.AddCell(cellQuantite);

                PdfPCell cellPrixUnitaire = new PdfPCell(new Phrase(item.PrixUnitaire.ToString("N0", fr), normalFont));
                cellPrixUnitaire.HorizontalAlignment = Element.ALIGN_RIGHT;
                table.AddCell(cellPrixUnitaire);

                PdfPCell cellRemise = new PdfPCell(new Phrase(item.Remise.ToString("N0", fr), normalFont));
                cellRemise.HorizontalAlignment = Element.ALIGN_RIGHT;
                table.AddCell(cellRemise);

                PdfPCell cellNet = new PdfPCell(new Phrase(lineNet.ToString("N0", fr), normalFont));
                cellNet.HorizontalAlignment = Element.ALIGN_RIGHT;
                table.AddCell(cellNet);
            }

            //foreach (var item in items)
            //{
            //    decimal lineBrut = item.Quantite * item.PrixUnitaire;
            //    decimal lineRemise = lineBrut * (item.Remise / 100m);
            //    decimal lineNet = lineBrut - lineRemise;

            //    totalHtAvantRemise += lineNet;

            //    table.AddCell(new Phrase(item.CodeArticle ?? "", normalFont));
            //    table.AddCell(new Phrase(item.Libelle ?? "", normalFont));
            //    table.AddCell(new Phrase(item.Quantite.ToString("N0"), normalFont, Element.ALIGN_LEFT));
            //    table.AddCell(new Phrase(item.PrixUnitaire.ToString("N0"), normalFont));
            //    table.AddCell(new Phrase(item.Remise.ToString("N0"), normalFont));
            //    table.AddCell(new Phrase(lineNet.ToString("N0"), normalFont));
            //}

            doc.Add(table);
            doc.Add(new Paragraph("\n"));

            // --- Application de la remise globale ---
            decimal remiseGlobale = invoice.Remisetht;
            decimal montantRemiseGlobale = 0m;
            decimal totalHtApresRemise = totalHtAvantRemise;

            if (remiseGlobale > 0)
            {
                montantRemiseGlobale = totalHtAvantRemise * (remiseGlobale / 100m);
                totalHtApresRemise -= montantRemiseGlobale;
            }
            // --- Recalcul du taux de TVA global à partir des articles ---
            decimal totalHtAvantRemiseGlobale = items.Sum(i =>
            {
                decimal brut = i.Quantite * i.PrixUnitaire;
                decimal remise = brut * (i.Remise / 100m);
                return brut - remise;
            });

            decimal totalTvaAvantRemise = items.Sum(i =>
            {
                decimal brut = i.Quantite * i.PrixUnitaire;
                decimal remise = brut * (i.Remise / 100m);
                decimal htLigne = brut - remise;
                return htLigne * (i.TauxTva / 100m);
            });

            // Taux TVA global moyen pondéré
            decimal tauxTvaGlobal = totalHtAvantRemiseGlobale > 0
                ? (totalTvaAvantRemise / totalHtAvantRemiseGlobale) * 100m
                : 0m;

            // --- Recalcul de la TVA sur le montant HT après remise globale ---
            decimal totalTva = totalHtApresRemise * (tauxTvaGlobal / 100m);

            // --- Calcul TTC ---
            decimal totalTTC = totalHtApresRemise + totalTva;

            // --- Taxes supplémentaires (après remise globale) ---
            decimal montantGlobalCustomTaxe = 0m;
            decimal montantCustomTaxesArticles = 0m;

            // Taxes globales
            if (globalCustomTaxes != null && globalCustomTaxes.Count > 0)
            {
                decimal tauxGlobal = globalCustomTaxes.Sum(t => t.Amount);
                montantGlobalCustomTaxe = totalTTC * (tauxGlobal / 100m);
            }

            // Taxes par article (basées sur TTC après remise globale)
            foreach (var item in items)
            {
                if (item.CustomTaxes != null && item.CustomTaxes.Count > 0)
                {
                    decimal lineBrut = item.Quantite * item.PrixUnitaire;
                    decimal lineRemise = lineBrut * (item.Remise / 100m);
                    decimal lineNet = lineBrut - lineRemise;

                    // Ajustement de la remise globale au prorata
                    decimal partRemiseGlobale = lineNet / totalHtAvantRemise * montantRemiseGlobale;
                    decimal lineApresRemiseGlobale = lineNet - partRemiseGlobale;

                    decimal lineTva = lineApresRemiseGlobale * (item.TauxTva / 100m);
                    decimal lineTtc = lineApresRemiseGlobale + lineTva;

                    montantCustomTaxesArticles += item.CustomTaxes.Sum(t => lineTtc * (t.Amount / 100m));
                }
            }

            // Total autres taxes
            decimal autresTaxes = montantCustomTaxesArticles + montantGlobalCustomTaxe;

            // --- Net à payer ---
            decimal netAPayer = totalTTC + autresTaxes;
            // --- Tableau des totaux ---
            PdfPTable totalTable = new PdfPTable(2);
            totalTable.HorizontalAlignment = Element.ALIGN_RIGHT;
            totalTable.WidthPercentage = 60;

            // Définit la largeur des colonnes (optionnel, mais rend le tout plus propre)
            totalTable.SetWidths(new float[] { 3f, 2f });

            // Style de cellule avec bordures
            PdfPCell MakeCell(string text, Font font, int alignment = Element.ALIGN_LEFT)
            {
                cell = new PdfPCell(new Phrase(text, font));
                cell.Border = PdfPCell.BOX; // Bordure sur tout le tour
                cell.HorizontalAlignment = alignment;
                cell.Padding = 5; // Un peu d'espace interne pour plus d'esthétique
                return cell;
            }

            // Ligne : Total HT
            totalTable.AddCell(MakeCell("Total HT :", subFont, Element.ALIGN_LEFT));
            totalTable.AddCell(MakeCell(totalHtAvantRemise.ToString("N0", fr), normalFont, Element.ALIGN_RIGHT));

            if (remiseGlobale > 0)
            {
                totalTable.AddCell(MakeCell($"Remise globale ({remiseGlobale}%) :", subFont, Element.ALIGN_LEFT));
                totalTable.AddCell(MakeCell("-" + montantRemiseGlobale.ToString("N0", fr), normalFont, Element.ALIGN_RIGHT));

                totalTable.AddCell(MakeCell("HT après remise :", subFont, Element.ALIGN_LEFT));
                totalTable.AddCell(MakeCell(totalHtApresRemise.ToString("N0", fr), normalFont, Element.ALIGN_RIGHT));
            }

            // Ligne : TVA
            totalTable.AddCell(MakeCell("TVA :", subFont, Element.ALIGN_LEFT));
            totalTable.AddCell(MakeCell(totalTva.ToString("N0", fr), normalFont, Element.ALIGN_RIGHT));

            // Ligne : TTC
            totalTable.AddCell(MakeCell("TTC :", subFont, Element.ALIGN_LEFT));
            totalTable.AddCell(MakeCell(totalTTC.ToString("N0", fr), normalFont, Element.ALIGN_RIGHT));

            if (autresTaxes > 0)
            {
                totalTable.AddCell(MakeCell("Autres taxes :", subFont, Element.ALIGN_LEFT));
                totalTable.AddCell(MakeCell(autresTaxes.ToString("N0", fr), normalFont, Element.ALIGN_RIGHT));
            }

            // Ligne : Net à payer (mise en valeur)
            PdfPCell cellNetLabel = MakeCell("Net à payer :", subFont, Element.ALIGN_LEFT);
            PdfPCell cellNetValue = MakeCell(netAPayer.ToString("N0", fr), subFont, Element.ALIGN_RIGHT);

            cellNetLabel.BackgroundColor = BaseColor.LIGHT_GRAY;
            cellNetValue.BackgroundColor = BaseColor.LIGHT_GRAY;

            totalTable.AddCell(cellNetLabel);
            totalTable.AddCell(cellNetValue);

            // Ajout du tableau au document
            doc.Add(totalTable);
            doc.Add(new Paragraph($"\n\n{Entreprise.Footer}", normalFont));
            doc.Close();


        }
    }
}
