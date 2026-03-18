using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VoiceAssistantForBlind.Models;
using System.Text.Json;

namespace VoiceAssistantForBlind.Services
{
    public class ResumePdfService
    {
        public byte[] GeneratePdf(ResumeRequest model)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.6f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x
                        .FontSize(11)
                        .FontColor(Colors.Black)
                        .FontFamily("Times New Roman"));

                    page.Content().Column(column =>
                    {
                        column.Spacing(2);

                        // ========== HEADER ==========
                        column.Item().Row(row =>
                        {
                            // Left column - Name and details
                            row.RelativeItem().Column(leftCol =>
                            {
                                leftCol.Item().Text(model.FullName ?? "")
                                    .FontSize(18).Bold();
                                
                                leftCol.Item().Text("Bachelor of Engineering").FontSize(10);
                                leftCol.Item().Text("in Computer Science Engineering").FontSize(10);
                                leftCol.Item().Text("Sri Ramakrishna Institute Of Technology, Coimbatore")
                                    .FontSize(10);
                            });

                            // Right column - Contact info (aligned right)
                            row.ConstantItem(280).Column(rightCol =>
                            {
                                rightCol.Spacing(2);
                                
                                if (!string.IsNullOrWhiteSpace(model.Email))
                                {
                                    rightCol.Item().Text(model.Email).FontSize(9).AlignRight();
                                }
                                
                                // ✅ GitHub with hyperlink in BLACK
                                if (!string.IsNullOrWhiteSpace(model.GitHub))
                                {
                                    rightCol.Item().Row(linkRow =>
                                    {
                                        linkRow.RelativeItem().AlignRight().Row(innerRow =>
                                        {
                                            innerRow.AutoItem().Text("GitHub: ").FontSize(9);
                                            innerRow.AutoItem().Hyperlink(model.GitHub).Text("Jaya30102003")
                                                .FontSize(9).FontColor(Colors.Black).Underline();
                                        });
                                    });
                                }
                                
                                // ✅ LinkedIn with hyperlink in BLACK
                                if (!string.IsNullOrWhiteSpace(model.LinkedIn))
                                {
                                    rightCol.Item().Row(linkRow =>
                                    {
                                        linkRow.RelativeItem().AlignRight().Row(innerRow =>
                                        {
                                            innerRow.AutoItem().Text("LinkedIn: ").FontSize(9);
                                            innerRow.AutoItem().Hyperlink(model.LinkedIn).Text("jayadharshini-iyyappan-mithra")
                                                .FontSize(9).FontColor(Colors.Black).Underline();
                                        });
                                    });
                                }
                                
                                if (!string.IsNullOrWhiteSpace(model.Phone))
                                {
                                    rightCol.Item().Text($"Phone: {model.Phone}").FontSize(9).AlignRight();
                                }
                            });
                        });

                        column.Item().PaddingBottom(4);

                        // ========== SUMMARY SECTION - REMOVED ==========
                        // The Summary section has been completely removed
                        // No more auto-generated summary from Languages

                        // ========== EDUCATION SECTION ==========
                        if (model.Education?.Any() == true)
                        {
                            column.Item().Component(new SectionTitleComponent("Education"));
                            
                            foreach (var edu in model.Education)
                            {
                                column.Item().Row(eduRow =>
                                {
                                    eduRow.RelativeItem().Text(edu.Degree ?? "").Bold().FontSize(11);
                                    eduRow.ConstantItem(100).AlignRight().Text(edu.Duration ?? "").FontSize(10);
                                });

                                if (!string.IsNullOrWhiteSpace(edu.Institution))
                                {
                                    column.Item().PaddingLeft(0).Text(edu.Institution).FontSize(10);
                                }

                                if (!string.IsNullOrWhiteSpace(edu.Highlights))
                                {
                                    try
                                    {
                                        var highlights = JsonSerializer.Deserialize<List<string>>(edu.Highlights);
                                        if (highlights?.Any() == true)
                                        {
                                            column.Item().PaddingLeft(15).Column(highCol =>
                                            {
                                                foreach (var highlight in highlights)
                                                {
                                                    highCol.Item().Text($"• {highlight}").FontSize(9);
                                                }
                                            });
                                        }
                                    }
                                    catch
                                    {
                                        column.Item().PaddingLeft(15).Text($"• {edu.Highlights}").FontSize(9);
                                    }
                                }
                                column.Item().PaddingBottom(4);
                            }
                        }

                        // ========== EXPERIENCE SECTION ==========
                        if (model.Experience?.Any() == true)
                        {
                            column.Item().Component(new SectionTitleComponent("Experience"));
                            
                            foreach (var exp in model.Experience)
                            {
                                // Company and Role line
                                if (!string.IsNullOrWhiteSpace(exp.Company) || !string.IsNullOrWhiteSpace(exp.Role))
                                {
                                    column.Item().Row(expRow =>
                                    {
                                        string title = "";
                                        if (!string.IsNullOrWhiteSpace(exp.Company) && !string.IsNullOrWhiteSpace(exp.Role))
                                            title = $"{exp.Company} - {exp.Role}";
                                        else if (!string.IsNullOrWhiteSpace(exp.Company))
                                            title = exp.Company;
                                        else if (!string.IsNullOrWhiteSpace(exp.Role))
                                            title = exp.Role;
                                            
                                        expRow.RelativeItem().Text(title).Bold().FontSize(11);
                                        expRow.ConstantItem(100).AlignRight().Text(exp.Duration ?? "").FontSize(10);
                                    });
                                }

                                // Location
                                if (!string.IsNullOrWhiteSpace(exp.Location))
                                {
                                    column.Item().Text(exp.Location).FontSize(10);
                                }

                                // Highlights
                                if (!string.IsNullOrWhiteSpace(exp.Highlights))
                                {
                                    try
                                    {
                                        var highlights = JsonSerializer.Deserialize<List<string>>(exp.Highlights);
                                        if (highlights?.Any() == true)
                                        {
                                            column.Item().PaddingLeft(15).Column(highCol =>
                                            {
                                                foreach (var highlight in highlights)
                                                {
                                                    highCol.Item().Text($"• {highlight}").FontSize(9);
                                                }
                                            });
                                        }
                                    }
                                    catch
                                    {
                                        column.Item().PaddingLeft(15).Text($"• {exp.Highlights}").FontSize(9);
                                    }
                                }
                                column.Item().PaddingBottom(4);
                            }
                        }

                        // ========== PROJECTS SECTION ==========
                        if (model.Projects?.Any() == true)
                        {
                            column.Item().Component(new SectionTitleComponent("Projects"));
                            
                            foreach (var proj in model.Projects)
                            {
                                column.Item().Row(projRow =>
                                {
                                    projRow.RelativeItem().Text(proj.Title ?? "").Bold().FontSize(11);
                                    projRow.ConstantItem(100).AlignRight().Text(proj.Duration ?? "").FontSize(10);
                                });

                                if (!string.IsNullOrWhiteSpace(proj.Highlights))
                                {
                                    try
                                    {
                                        var highlights = JsonSerializer.Deserialize<List<string>>(proj.Highlights);
                                        if (highlights?.Any() == true)
                                        {
                                            column.Item().PaddingLeft(15).Column(highCol =>
                                            {
                                                foreach (var highlight in highlights)
                                                {
                                                    highCol.Item().Text($"• {highlight}").FontSize(9);
                                                }
                                            });
                                        }
                                    }
                                    catch
                                    {
                                        column.Item().PaddingLeft(15).Text($"• {proj.Highlights}").FontSize(9);
                                    }
                                }
                                column.Item().PaddingBottom(4);
                            }
                        }

                        // ========== TECHNICAL SKILLS SECTION ==========
                        bool hasSkills = !string.IsNullOrWhiteSpace(model.Languages) || 
                                         !string.IsNullOrWhiteSpace(model.Concepts) || 
                                         !string.IsNullOrWhiteSpace(model.Software);
                        
                        if (hasSkills)
                        {
                            column.Item().Component(new SectionTitleComponent("Technical Skills"));
                            
                            if (!string.IsNullOrWhiteSpace(model.Languages))
                            {
                                column.Item().Row(row =>
                                {
                                    row.ConstantItem(120).Text("Programming Languages:").Bold().FontSize(10);
                                    row.RelativeItem().Text(model.Languages).FontSize(10);
                                });
                            }

                            if (!string.IsNullOrWhiteSpace(model.Concepts))
                            {
                                column.Item().Row(row =>
                                {
                                    row.ConstantItem(120).Text("Backend & AI Technologies:").Bold().FontSize(10);
                                    row.RelativeItem().Text(model.Concepts).FontSize(10);
                                });
                            }

                            if (!string.IsNullOrWhiteSpace(model.Software))
                            {
                                column.Item().Row(row =>
                                {
                                    row.ConstantItem(120).Text("Cloud & Platforms:").Bold().FontSize(10);
                                    row.RelativeItem().Text(model.Software).FontSize(10);
                                });
                            }
                            column.Item().PaddingBottom(4);
                        }

                        // ========== CERTIFICATIONS SECTION ==========
                        if (model.Certifications?.Any(c => !string.IsNullOrWhiteSpace(c)) == true)
                        {
                            column.Item().Component(new SectionTitleComponent("Certifications"));
                            
                            foreach (var cert in model.Certifications.Where(c => !string.IsNullOrWhiteSpace(c)))
                            {
                                column.Item().PaddingLeft(15).Text($"• {cert}").FontSize(9);
                            }
                            column.Item().PaddingBottom(4);
                        }

                        // ========== ACHIEVEMENTS SECTION ==========
                        if (model.Achievements?.Any(a => !string.IsNullOrWhiteSpace(a)) == true)
                        {
                            column.Item().Component(new SectionTitleComponent("Achievements"));
                            
                            foreach (var ach in model.Achievements.Where(a => !string.IsNullOrWhiteSpace(a)))
                            {
                                column.Item().PaddingLeft(15).Text($"• {ach}").FontSize(9);
                            }
                            column.Item().PaddingBottom(4);
                        }
                    });
                });
            }).GeneratePdf();
        }
    }

    public class SectionTitleComponent : IComponent
    {
        private string Title { get; }

        public SectionTitleComponent(string title)
        {
            Title = title;
        }

        public void Compose(IContainer container)
        {
            container.Background(Colors.Grey.Lighten3).Padding(4).Row(row =>
            {
                row.RelativeItem().Text(Title).Bold().FontSize(12);
            });
        }
    }
}