using HierarchicalTaskApp.Models;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System;

namespace HierarchicalTaskApp.Reports
{
    public class TeamTaskReport : IDocument
    {
        private readonly ReportViewModel _model;

        public TeamTaskReport(ReportViewModel model)
        {
            _model = model;
        }

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Margin(30);
                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });
        }

        // --- BÖLÜMLER ---

        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"{_model.ReportingUser.FullName} - Ekip Görev Raporu")
                        .Bold().FontSize(20);
                    
                    col.Item().Text($"Rapor Tarihi: {DateTime.Now.ToString("dd MMMM yyyy, HH:mm")}")
                        .FontSize(10);
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(20).Column(col =>
            {
                col.Item().Element(ComposeSummaries);
                col.Item().PaddingVertical(10);
                col.Item().Element(ComposeTaskTable);
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(text =>
            {
                text.CurrentPageNumber().FontSize(10);
                text.Span(" / ").FontSize(10);
                text.TotalPages().FontSize(10);
            });
        }

        // --- İÇERİK DETAYLARI ---

        void ComposeSummaries(IContainer container)
        {
            // --- DÜZELTME BURADA (CS0104 HATALARI İÇİN) ---
            var tasksByStatus = _model.Tasks
                .GroupBy(t => t.Status)
                .Select(g => new { 
                    StatusName = g.Key switch {
                        // Tam namespace kullanıldı
                        HierarchicalTaskApp.Models.TaskStatus.Done => "Tamamlandı",
                        HierarchicalTaskApp.Models.TaskStatus.InProgress => "Devam Ediyor",
                        _ => "Beklemede"
                    },
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count) 
                .ToList();
            // --- DÜZELTME BİTTİ ---

            var tasksByCategory = _model.Tasks
                .GroupBy(t => t.CategoryId)
                .Select(g => new {
                    CategoryName = _model.AllCategories.FirstOrDefault(c => c.Id == g.Key)?.Name ?? "Bilinmeyen Kategori",
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            var tasksByAssignerDept = _model.Tasks
                .Select(t => {
                    var assigner = _model.AllUsers.FirstOrDefault(u => u.Id == t.AssignerId);
                    var deptName = _model.AllDepartments.FirstOrDefault(d => d.Id == assigner?.DepartmentId)?.Name ?? "Sistem/Diğer";
                    return new { DeptName = deptName };
                })
                .GroupBy(x => x.DeptName)
                .Select(g => new { DeptName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();


            container.Row(row =>
            {
                row.RelativeItem(5).Column(col =>
                {
                    col.Item().Element(c => ComposeSummaryBox(c, "Görev Durum Özeti (Hiyerarşi)", tasksByStatus));
                    col.Item().PaddingTop(10).Element(c => ComposeSummaryBox(c, "Sorunlu Kategoriler (Top 5)", tasksByCategory.Take(5)));
                });
                
                row.RelativeItem(5).PaddingLeft(10).Element(c => ComposeSummaryBox(c, "Görevlerin Geldiği Departmanlar", tasksByAssignerDept));
            });
        }
        
        void ComposeTaskTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.5f); // Öncelik
                    columns.RelativeColumn(3);    // Başlık
                    columns.RelativeColumn(2.5f); // Atanan
                    columns.RelativeColumn(2.5f); // Kategori
                    columns.RelativeColumn(2);    // Durum
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Öncelik");
                    header.Cell().Element(CellStyle).Text("Başlık");
                    header.Cell().Element(CellStyle).Text("Atanan");
                    header.Cell().Element(CellStyle).Text("Kategori");
                    header.Cell().Element(CellStyle).Text("Durum");
                });
                
                var sortedTasks = _model.Tasks
                    .OrderByDescending(t => t.Flag)
                    .ThenByDescending(t => t.Priority)
                    .ThenBy(t => t.Status);

                foreach (var task in sortedTasks)
                {
                    table.Cell().Element(c => TableCell(c, GetPriorityText(task.Priority), task.Flag));
                    table.Cell().Element(c => TableCell(c, task.Title, task.Flag));
                    table.Cell().Element(c => TableCell(c, _model.AllUsers.FirstOrDefault(u => u.Id == task.AssigneeId)?.FullName ?? "N/A", task.Flag));
                    table.Cell().Element(c => TableCell(c, _model.AllCategories.FirstOrDefault(c => c.Id == task.CategoryId)?.Name ?? "N/A", task.Flag));
                    
                    // --- DÜZELTME BURADA (CS1503 HATASI İÇİN) ---
                    // task.Status'ın tipi artık net olduğu için GetStatusText metodu doğru çalışacak
                    table.Cell().Element(c => TableCell(c, GetStatusText(task.Status), task.Flag));
                }
            });
        }
        
        // --- YARDIMCI METOTLAR (Stiller ve Metinler) ---

        void ComposeSummaryBox(IContainer container, string title, IEnumerable<dynamic> items)
        {
            container
                .Border(1)
                .Background(Colors.Grey.Lighten4)
                .Padding(5)
                .Column(col =>
                {
                    col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(2).Text(title).Bold();
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                        });
                        
                        foreach (var item in items)
                        {
                            string name = item.GetType().GetProperty(item.GetType().GetProperties()[0].Name).GetValue(item).ToString();
                            string count = item.GetType().GetProperty(item.GetType().GetProperties()[1].Name).GetValue(item).ToString();

                            table.Cell().Text(name).FontSize(9);
                            table.Cell().Text(count).FontSize(9).Bold();
                        }
                    });
                });
        }
        
        IContainer CellStyle(IContainer container)
        {
            return container.DefaultTextStyle(x => x.Bold()).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Background(Colors.Grey.Lighten3);
        }
        
        void TableCell(IContainer container, string text, FlagStatus flag)
        {
            var style = TextStyle.Default.FontSize(9);
            if (flag == FlagStatus.FlaggedAsIncorrect)
            {
                style = style.FontColor(Colors.Red.Medium).Bold();
            }

            container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten3)
                .Padding(5)
                .Text(text)
                .Style(style);
        }

        private string GetPriorityText(TaskPriority priority) => priority switch
        {
            TaskPriority.Urgent => "Acil",
            TaskPriority.High => "Yüksek",
            TaskPriority.Low => "Düşük",
            _ => "Normal"
        };
        
        // --- DÜZELTME BURADA (CS0104 HATALARI İÇİN) ---
        // 'TaskStatus' parametresine tam namespace eklendi
        private string GetStatusText(HierarchicalTaskApp.Models.TaskStatus status) => status switch
        {
            HierarchicalTaskApp.Models.TaskStatus.Done => "Tamamlandı",
            HierarchicalTaskApp.Models.TaskStatus.InProgress => "Devam Ediyor",
            _ => "Beklemede"
        };
    }
}