#!/usr/bin/env python3
"""Render the QM Markdown report to PDF. Requires reportlab (tested with 5.0.1)."""
from pathlib import Path
import re
from xml.sax.saxutils import escape
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle

root = Path(__file__).resolve().parents[1]
source = root / 'docs/qm/SOFTWARE_TESTING_REPORT.md'
output = source.with_suffix('.pdf')
styles = getSampleStyleSheet()
styles['BodyText'].fontSize = 10
styles['BodyText'].leading = 14
styles['BodyText'].spaceAfter = 7
styles['BodyText'].alignment = TA_LEFT
styles['Title'].fontSize = 21
styles['Heading2'].fontSize = 13
styles['Heading2'].spaceBefore = 13
styles['Heading2'].keepWithNext = True

def markup(text):
    text = escape(text)
    text = re.sub(r'\*\*(.+?)\*\*', r'<b>\1</b>', text)
    return re.sub(r'`([^`]+)`', r'<font name="Courier">\1</font>', text)

def paragraph(text):
    return Paragraph(markup(text), styles['BodyText'])

story = []
lines = source.read_text().splitlines()
i = 0
while i < len(lines):
    line = lines[i].strip()
    if not line:
        i += 1
        continue
    if line.startswith('|'):
        rows = []
        while i < len(lines) and lines[i].strip().startswith('|'):
            cells = [c.strip() for c in lines[i].strip().strip('|').split('|')]
            if not all(re.fullmatch(r':?-+:?', c) for c in cells):
                rows.append([paragraph(c) for c in cells])
            i += 1
        widths = [135, 360] if len(rows[0]) == 2 else [165] * 3
        table = Table(rows, colWidths=widths, repeatRows=1, hAlign='LEFT')
        table.setStyle(TableStyle([
            ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#e4f1f1')),
            ('GRID', (0, 0), (-1, -1), .4, colors.HexColor('#b8c5c5')),
            ('VALIGN', (0, 0), (-1, -1), 'TOP'),
            ('LEFTPADDING', (0, 0), (-1, -1), 7),
            ('RIGHTPADDING', (0, 0), (-1, -1), 7),
            ('TOPPADDING', (0, 0), (-1, -1), 6),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 6),
        ]))
        story.extend([table, Spacer(1, 9)])
        continue
    if line.startswith('# '):
        story.append(Paragraph(markup(line[2:]), styles['Title']))
    elif line.startswith('## '):
        story.append(Paragraph(markup(line[3:]), styles['Heading2']))
    else:
        story.append(paragraph(line))
    i += 1

def footer(canvas, doc):
    canvas.saveState()
    canvas.setFont('Helvetica', 8)
    canvas.drawString(50, 27, 'CarePulse | Software Testing Report | 3 October 2026')
    canvas.drawRightString(A4[0] - 50, 27, str(doc.page))
    canvas.restoreState()

SimpleDocTemplate(str(output), pagesize=A4, rightMargin=50, leftMargin=50,
                  topMargin=42, bottomMargin=45,
                  title='CarePulse Software Testing Report', author='CarePulse project team').build(
                      story, onFirstPage=footer, onLaterPages=footer)
print(output)
