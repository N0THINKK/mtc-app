using System;
using System.Data;
using System.Threading.Tasks;
using mtc_app.features.micrometer_patrol.data.repositories;
using mtc_app.features.micrometer_patrol.data.dtos;
using System.Collections.Generic;

namespace mtc_app.features.micrometer_patrol.presentation.controllers
{
    public class MicrometerPatrolHistoryController
    {
        private readonly IMicrometerPatrolHistoryView _view;
        private readonly IMicrometerPatrolRepository _repository;

        public MicrometerPatrolHistoryController(IMicrometerPatrolHistoryView view, IMicrometerPatrolRepository repository)
        {
            _view = view;
            _repository = repository;
        }

        public async Task LoadHistoryDataAsync()
        {
            try
            {
                _view.ShowLoading();

                var data = await _repository.GetTodayPatrolsAsync(DateTime.Now);
                var listData = new List<MicrometerPatrolDto>(data);

                if (listData.Count == 0)
                {
                    _view.SetStatusMessage("Belum ada riwayat hari ini.");
                }
                else
                {
                    DataTable pivotTable = BuildPivotTable(listData);
                    _view.DisplayData(pivotTable);
                }
            }
            catch (Exception ex)
            {
                _view.SetStatusMessage("Gagal memuat history: " + ex.Message, isError: true);
            }
        }

        private DataTable BuildPivotTable(List<MicrometerPatrolDto> listData)
        {
            DataTable pivotTable = new DataTable();
            pivotTable.Columns.Add("Checksheet Item", typeof(string));

            foreach (var record in listData)
            {
                string colName = $"{record.PatrolDate.ToString("dd/MM/yyyy")} ({record.ShiftName})";
                if (!pivotTable.Columns.Contains(colName))
                {
                    pivotTable.Columns.Add(colName, typeof(string));
                }
            }

            string[] points = new string[] 
            {
                "1. Ada Nomer Registrasi dan tidak Expired",
                "2. Angka terbaca dengan jelas",
                "3. Zero setting OK",
                "4. Kondisi Thimble, Anvil dan Spindle OK",
                "5. Baut Pengunci tidak longgar/Dol"
            };

            for (int i = 0; i < 5; i++)
            {
                var row = pivotTable.NewRow();
                row[0] = points[i];

                foreach (var record in listData)
                {
                    string colName = $"{record.PatrolDate.ToString("dd/MM/yyyy")} ({record.ShiftName})";
                    string val = "";
                    if (i == 0) val = record.Point1;
                    if (i == 1) val = record.Point2;
                    if (i == 2) val = record.Point3;
                    if (i == 3) val = record.Point4;
                    if (i == 4) val = record.Point5;

                    row[colName] = val;
                }
                pivotTable.Rows.Add(row);
            }

            var noteRow = pivotTable.NewRow();
            noteRow[0] = "Keterangan";
            foreach (var record in listData)
            {
                string colName = $"{record.PatrolDate.ToString("dd/MM/yyyy")} ({record.ShiftName})";
                noteRow[colName] = record.Notes;
            }
            pivotTable.Rows.Add(noteRow);

            return pivotTable;
        }
    }
}
