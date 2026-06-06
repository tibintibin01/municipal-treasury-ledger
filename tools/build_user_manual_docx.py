import re
from pathlib import Path

from docx import Document
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Inches, Pt, RGBColor


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "USER_MANUAL.md"
OUTPUT = ROOT / "Business_Tax_Permit_Collection_System_User_Manual.docx"


ACCENT = "0F766E"
DARK = "1F2937"
MUTED = "6B7280"
WARNING = "92400E"


def set_paragraph_border(paragraph, color="CBD5E1", size="6"):
    p_pr = paragraph._p.get_or_add_pPr()
    p_bdr = p_pr.find(qn("w:pBdr"))
    if p_bdr is None:
        p_bdr = OxmlElement("w:pBdr")
        p_pr.append(p_bdr)
    bottom = OxmlElement("w:bottom")
    bottom.set(qn("w:val"), "single")
    bottom.set(qn("w:sz"), size)
    bottom.set(qn("w:space"), "4")
    bottom.set(qn("w:color"), color)
    p_bdr.append(bottom)


def configure_styles(doc):
    styles = doc.styles

    normal = styles["Normal"]
    normal.font.name = "Calibri"
    normal.font.size = Pt(11)
    normal.font.color.rgb = RGBColor.from_string(DARK)
    normal.paragraph_format.space_after = Pt(6)
    normal.paragraph_format.line_spacing = 1.15

    for style_name, size, color, before, after in [
        ("Heading 1", 16, ACCENT, 16, 8),
        ("Heading 2", 13, DARK, 10, 5),
        ("Heading 3", 12, DARK, 8, 4),
    ]:
        style = styles[style_name]
        style.font.name = "Calibri"
        style.font.size = Pt(size)
        style.font.bold = True
        style.font.color.rgb = RGBColor.from_string(color)
        style.paragraph_format.space_before = Pt(before)
        style.paragraph_format.space_after = Pt(after)

    for style_name in ["List Bullet", "List Number"]:
        style = styles[style_name]
        style.font.name = "Calibri"
        style.font.size = Pt(11)
        style.font.color.rgb = RGBColor.from_string(DARK)
        style.paragraph_format.space_after = Pt(3)


def add_inline_text(paragraph, text):
    token_re = re.compile(r"(`[^`]+`|\*\*[^*]+\*\*)")
    pos = 0
    for match in token_re.finditer(text):
        if match.start() > pos:
            paragraph.add_run(text[pos : match.start()])
        token = match.group(0)
        if token.startswith("`"):
            run = paragraph.add_run(token[1:-1])
            run.font.name = "Consolas"
            run.font.size = Pt(10)
        elif token.startswith("**"):
            run = paragraph.add_run(token[2:-2])
            run.bold = True
        pos = match.end()
    if pos < len(text):
        paragraph.add_run(text[pos:])


def add_cover(doc):
    title = doc.add_paragraph()
    title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    title.paragraph_format.space_after = Pt(8)
    run = title.add_run("Business Tax & Permit Collection System")
    run.bold = True
    run.font.name = "Calibri"
    run.font.size = Pt(24)
    run.font.color.rgb = RGBColor.from_string(ACCENT)

    subtitle = doc.add_paragraph()
    subtitle.alignment = WD_ALIGN_PARAGRAPH.CENTER
    subtitle.paragraph_format.space_after = Pt(8)
    run = subtitle.add_run("Office User Manual and Daily Workflow Guide")
    run.font.name = "Calibri"
    run.font.size = Pt(16)
    run.font.color.rgb = RGBColor.from_string(DARK)

    meta = doc.add_paragraph()
    meta.alignment = WD_ALIGN_PARAGRAPH.CENTER
    meta.paragraph_format.space_after = Pt(18)
    run = meta.add_run("Version v0.3.44\nPrepared for Municipal Treasurer's Office staff")
    run.font.name = "Calibri"
    run.font.size = Pt(11)
    run.font.color.rgb = RGBColor.from_string(MUTED)

    note = doc.add_paragraph()
    note.alignment = WD_ALIGN_PARAGRAPH.CENTER
    note.paragraph_format.left_indent = Inches(0.35)
    note.paragraph_format.right_indent = Inches(0.35)
    note.paragraph_format.space_before = Pt(8)
    note.paragraph_format.space_after = Pt(16)
    run = note.add_run(
        "This manual is written for real office use. Follow the daily workflow, keep backups current, "
        "and ask an Admin or Treasurer before restoring backups, deleting records, importing large files, "
        "or changing system settings."
    )
    run.italic = True
    run.font.name = "Calibri"
    run.font.size = Pt(11)
    run.font.color.rgb = RGBColor.from_string(DARK)
    set_paragraph_border(note, ACCENT, "10")


def add_note(doc, line):
    p = doc.add_paragraph()
    p.paragraph_format.left_indent = Inches(0.25)
    p.paragraph_format.right_indent = Inches(0.25)
    p.paragraph_format.space_before = Pt(4)
    p.paragraph_format.space_after = Pt(8)
    run = p.add_run(line)
    run.bold = True
    run.font.name = "Calibri"
    run.font.size = Pt(10.5)
    run.font.color.rgb = RGBColor.from_string(WARNING)
    set_paragraph_border(p, "F59E0B", "8")


def add_screenshot_placeholder(doc, line):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(4)
    p.paragraph_format.space_after = Pt(10)
    run = p.add_run(line)
    run.italic = True
    run.font.name = "Calibri"
    run.font.size = Pt(10)
    run.font.color.rgb = RGBColor.from_string(MUTED)
    set_paragraph_border(p, "CBD5E1", "6")


def add_markdown_line(doc, line):
    if line.startswith("[Screenshot:"):
        add_screenshot_placeholder(doc, line)
        return
    if line.startswith("# "):
        doc.add_heading(line[2:].strip(), level=1)
        return
    if line.startswith("## "):
        doc.add_heading(line[3:].strip(), level=1)
        return
    if line.startswith("### "):
        doc.add_heading(line[4:].strip(), level=2)
        return
    if line.startswith("- "):
        p = doc.add_paragraph(style="List Bullet")
        add_inline_text(p, line[2:].strip())
        return
    if re.match(r"^\d+\.\s+", line):
        p = doc.add_paragraph(style="List Number")
        add_inline_text(p, re.sub(r"^\d+\.\s+", "", line).strip())
        return
    if line.startswith("Important:") or line.startswith("Note:") or line.startswith("Warning:"):
        add_note(doc, line)
        return

    p = doc.add_paragraph()
    add_inline_text(p, line)


def build_docx():
    markdown = SOURCE.read_text(encoding="utf-8")
    lines = markdown.splitlines()

    doc = Document()
    section = doc.sections[0]
    section.top_margin = Inches(0.75)
    section.bottom_margin = Inches(0.75)
    section.left_margin = Inches(0.85)
    section.right_margin = Inches(0.85)
    configure_styles(doc)

    add_cover(doc)

    # The first eight source lines are the title block already represented on the cover.
    for raw in lines[8:]:
        line = raw.rstrip()
        if not line:
            continue
        add_markdown_line(doc, line)

    for section in doc.sections:
        footer = section.footer.paragraphs[0]
        footer.alignment = WD_ALIGN_PARAGRAPH.CENTER
        run = footer.add_run("Business Tax & Permit Collection System User Manual")
        run.font.name = "Calibri"
        run.font.size = Pt(9)
        run.font.color.rgb = RGBColor.from_string(MUTED)

    doc.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    build_docx()
