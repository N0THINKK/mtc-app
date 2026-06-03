using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.features.operator_worksheet.presentation.controllers;
using mtc_app.features.operator_worksheet.services;
using mtc_app.shared.data.dtos;
using mtc_app.features.operator_worksheet.data.dtos;

namespace mtc_app.features.operator_worksheet.presentation.screens
{
    public partial class OperatorWorksheetForm : IOperatorWorksheetView
    {
        public void UpdateSequenGrid(List<LkoService.LkoAggregatedData> data)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateSequenGrid(data)));
                return;
            }
            int firstRowSeq = _dgvSequen.RowCount > 0 ? _dgvSequen.FirstDisplayedScrollingRowIndex : -1;
            int selRowSeq = _dgvSequen.CurrentRow?.Index ?? -1;

            _worksheetData = data;
            _dgvSequen.DataSource = _worksheetData;

            if (firstRowSeq >= 0 && firstRowSeq < _dgvSequen.RowCount)
                _dgvSequen.FirstDisplayedScrollingRowIndex = firstRowSeq;
            if (selRowSeq >= 0 && selRowSeq < _dgvSequen.RowCount)
            {
                _dgvSequen.ClearSelection();
                _dgvSequen.Rows[selRowSeq].Selected = true;
            }
        }

        public void UpdateProductGrid(List<ProductDto> pendingProducts)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateProductGrid(pendingProducts)));
                return;
            }
            int firstRowProd = _dgvProduct.RowCount > 0 ? _dgvProduct.FirstDisplayedScrollingRowIndex : -1;
            int selRowProd = _dgvProduct.CurrentRow?.Index ?? -1;

            _dgvProduct.DataSource = pendingProducts;

            if (firstRowProd >= 0 && firstRowProd < _dgvProduct.RowCount)
                _dgvProduct.FirstDisplayedScrollingRowIndex = firstRowProd;
            if (selRowProd >= 0 && selRowProd < _dgvProduct.RowCount)
            {
                _dgvProduct.ClearSelection();
                _dgvProduct.Rows[selRowProd].Selected = true;
            }
            else
            {
                _dgvProduct.ClearSelection();
            }
        }

        public void UpdateTersimpanGrid(List<LkoRecordDto> tersimpanData)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateTersimpanGrid(tersimpanData)));
                return;
            }
            _dgvTersimpan.DataSource = tersimpanData;
            _dgvSequen.Refresh(); // Refresh color highlighting
        }

        public void UpdateHeaderQty(int grossSum, int netSum, int defectSum)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateHeaderQty(grossSum, netSum, defectSum)));
                return;
            }
            _pbGrossQty.Maximum = Math.Max(grossSum, 1);
            _pbGrossQty.Value = Math.Min(grossSum, _pbGrossQty.Maximum);
            
            _pbNetQty.Maximum = Math.Max(grossSum, 1);
            _pbNetQty.Value = Math.Min(Math.Max(netSum, 0), _pbNetQty.Maximum);
            
            _lblGrossQty.Text = $"Gross: {grossSum}";
            _lblNetQty.Text = $"OK: {netSum}";

            _lblNetQty.Left = _pbNetQty.Left - TextRenderer.MeasureText(_lblNetQty.Text, _lblNetQty.Font).Width - 8;
            _pbGrossQty.Left = _lblNetQty.Left - _pbGrossQty.Width - 16;
            _lblGrossQty.Left = _pbGrossQty.Left - TextRenderer.MeasureText(_lblGrossQty.Text, _lblGrossQty.Font).Width - 8;
        }

        public void SetIsXmlSource(bool isXml)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetIsXmlSource(isXml)));
                return;
            }
            if (isXml)
            {
                if (_dgvSequen.Columns.Contains("Urutan")) _dgvSequen.Columns["Urutan"].HeaderText = "Waktu";
                if (_dgvTersimpan.Columns.Contains("Urutan")) _dgvTersimpan.Columns["Urutan"].HeaderText = "Waktu";
            }
            else
            {
                if (_dgvSequen.Columns.Contains("Urutan")) _dgvSequen.Columns["Urutan"].HeaderText = "Urutan";
                if (_dgvTersimpan.Columns.Contains("Urutan")) _dgvTersimpan.Columns["Urutan"].HeaderText = "Urutan";
            }
        }

        public void RunOnUIThread(Action action)
        {
            if (this.InvokeRequired) this.BeginInvoke(action);
            else action();
        }
        
        public string GetEffectiveMachineNumber()
        {
            // Implementasi ada di OperatorWorksheetForm.cs utama, jadi interface bisa mengaksesnya
            return this.GetEffectiveMachineNumberInternal();
        }
    }
}
