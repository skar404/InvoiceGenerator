using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InvoiceGenerator.Controllers;

public class InvoiceModel
{
    public int InvoiceNumber { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }

    public Address SellerAddress { get; set; }
    public Address CustomerAddress { get; set; }

    public List<OrderItem> Items { get; set; }
    public string Comments { get; set; }
}

public class OrderItem
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class Address
{
    public string CompanyName { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
}

public static class InvoiceDocumentDataSource
{
    private static Random Random = new Random();

    public static InvoiceModel GetInvoiceDetails()
    {
        var items = Enumerable
            .Range(1, 8)
            .Select(i => GenerateRandomOrderItem())
            .ToList();

        return new InvoiceModel
        {
            InvoiceNumber = Random.Next(1_000, 10_000),
            IssueDate = DateTime.Now,
            DueDate = DateTime.Now + TimeSpan.FromDays(14),

            SellerAddress = GenerateRandomAddress(),
            CustomerAddress = GenerateRandomAddress(),

            Items = items,
            Comments = Placeholders.Paragraph()
        };
    }

    private static OrderItem GenerateRandomOrderItem()
    {
        return new OrderItem
        {
            Name = Placeholders.Label(),
            Price = (decimal)Math.Round(Random.NextDouble() * 100, 2),
            Quantity = Random.Next(1, 10)
        };
    }

    private static Address GenerateRandomAddress()
    {
        return new Address
        {
            CompanyName = Placeholders.Name(),
            Street = Placeholders.Label(),
            City = Placeholders.Label(),
            State = Placeholders.Label()
        };
    }
}

public interface IDocument
{
    DocumentMetadata GetMetadata();
    DocumentSettings GetSettings();
    void Compose(IDocumentContainer container);
}

public interface IComponent
{
    void Compose(IContainer container);
}

public class AddressComponent : IComponent, QuestPDF.Infrastructure.IComponent
{
    private string Title { get; }
    private Address Address { get; }

    public AddressComponent(string title, Address address)
    {
        Title = title;
        Address = address;
    }

    public void Compose(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(2);

            column.Item().BorderBottom(1).PaddingBottom(5).Text(Title).SemiBold();

            column.Item().Text(Address.CompanyName);
            column.Item().Text(Address.Street);
            column.Item().Text($"{Address.City} {Address.State}");
        });
    }
}

public class InvoiceDocument : IDocument
{
    public InvoiceModel Model { get; }

    public InvoiceDocument(InvoiceModel model)
    {
        Model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Margin(50);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);


                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
    }

    void ComposeHeader(IContainer container)
    {
        var totalPrice = Model.Items.Sum(x => x.Price * x.Quantity);
        var titleStyle = TextStyle.Default.FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text($"INVOICE #{Model.InvoiceNumber}").Style(titleStyle);

                column.Item().Text(text =>
                {
                    text.Span("Date: ").SemiBold();
                    text.Span($"{Model.IssueDate:d}");
                });

                column.Item().Text(text =>
                {
                    text.Span("Due date: ").SemiBold();
                    text.Span($"{Model.DueDate:d}");
                });
                column.Item().Text(text =>
                {
                    text.Span("Balance Due: ").SemiBold();
                    text.Span($"£{totalPrice}");
                });
            });
        });
    }

    void ComposeContent(IContainer container)
    {
        container.PaddingVertical(40).Column(column =>
        {
            column.Spacing(5);

            column.Item().Row(row =>
            {
                row.RelativeItem().Component(new AddressComponent("From:", Model.SellerAddress));
                row.ConstantItem(50);
                row.RelativeItem().Component(new AddressComponent("Bill To:", Model.CustomerAddress));
            });

            column.Item().Element(ComposeTable);

            var totalPrice = Model.Items.Sum(x => x.Price * x.Quantity);
            column.Item().AlignRight().Text($"Subtotal: £{totalPrice}").FontSize(12);
            column.Item().AlignRight().Text("Tax (0%): £0.00").FontSize(12);
            column.Item().AlignRight().Text($"Grand total: £{totalPrice}").FontSize(12);


            if (!string.IsNullOrWhiteSpace(Model.Comments))
                column.Item().PaddingTop(25).Element(ComposeComments);
        });
    }

    void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            // step 1
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(25);
                columns.RelativeColumn(3);
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            // step 2
            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("#");
                header.Cell().Element(CellStyle).Text("Item");
                header.Cell().Element(CellStyle).AlignRight().Text("Rate");
                header.Cell().Element(CellStyle).AlignRight().Text("Quantity");
                header.Cell().Element(CellStyle).AlignRight().Text("Amount");

                static IContainer CellStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1)
                        .BorderColor(Colors.Black);
                }
            });

            // step 3
            foreach (var item in Model.Items)
            {
                table.Cell().Element(CellStyle).Text(Model.Items.IndexOf(item) + 1);
                table.Cell().Element(CellStyle).Text(item.Name);
                table.Cell().Element(CellStyle).AlignRight().Text($"£{item.Price}");
                table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity);
                table.Cell().Element(CellStyle).AlignRight().Text($"£{item.Price * item.Quantity}");

                static IContainer CellStyle(IContainer container)
                {
                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
            }
        });
    }

    void ComposeComments(IContainer container)
    {
        container.Background(Colors.Grey.Lighten3).Padding(10).Column(column =>
        {
            column.Spacing(5);
            column.Item().Text("Notes").FontSize(14);
            column.Item().Text(Model.Comments);
        });
    }
}

[ApiController]
[Route("invoice")]
public class InvoiceController : ControllerBase
{
    [HttpPost(Name = "CreateInvoice")]
    public IActionResult GetInvoice([FromBody] InvoiceModel invoiceModel)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = new InvoiceDocument(invoiceModel);

        var stream = new MemoryStream();

        Document.Create(document.Compose).GeneratePdf(stream);

        stream.Position = 0;
        return new FileStreamResult(stream, "application/pdf");
    }
}