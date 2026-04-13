using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.shared.presentation.components
{
    public class PaginationControl : UserControl
    {
        private FlowLayoutPanel _flowLayoutPanel;
        private int _currentPage = 1;
        private int _totalPages = 1;
        
        public event EventHandler<int> PageChanged;

        public PaginationControl()
        {
            this.Height = 36;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.BackColor = Color.Transparent;

            _flowLayoutPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            this.Controls.Add(_flowLayoutPanel);
        }

        public void Setup(int totalItems, int pageSize, int currentPage)
        {
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            
            _totalPages = totalPages;
            _currentPage = currentPage;

            if (_currentPage > _totalPages) _currentPage = _totalPages;
            if (_currentPage < 1) _currentPage = 1;

            RenderPagination();
        }

        private void RenderPagination()
        {
            _flowLayoutPanel.SuspendLayout();
            
            // Dispose old controls to prevent memory leaks
            foreach (Control ctrl in _flowLayoutPanel.Controls)
            {
                ctrl.Dispose();
            }
            _flowLayoutPanel.Controls.Clear();

            // Prev Button
            _flowLayoutPanel.Controls.Add(CreateNavigateButton("<", _currentPage > 1, () => FirePageChanged(_currentPage - 1)));

            // Page 1
            _flowLayoutPanel.Controls.Add(CreatePageButton(1));

            // Left Ellipsis
            if (_currentPage > 3)
            {
                _flowLayoutPanel.Controls.Add(CreateEllipsis());
            }

            // Middle pages
            int startPage = Math.Max(2, _currentPage - 1);
            int endPage = Math.Min(_totalPages - 1, _currentPage + 1);

            for (int i = startPage; i <= endPage; i++)
            {
                _flowLayoutPanel.Controls.Add(CreatePageButton(i));
            }

            // Right Ellipsis
            if (_currentPage < _totalPages - 2)
            {
                _flowLayoutPanel.Controls.Add(CreateEllipsis());
            }

            // Last Page
            if (_totalPages > 1)
            {
                _flowLayoutPanel.Controls.Add(CreatePageButton(_totalPages));
            }

            // Next Button
            _flowLayoutPanel.Controls.Add(CreateNavigateButton(">", _currentPage < _totalPages, () => FirePageChanged(_currentPage + 1)));

            _flowLayoutPanel.ResumeLayout();
            this.PerformLayout();
        }

        private void FirePageChanged(int page)
        {
            PageChanged?.Invoke(this, page);
        }

        private Control CreatePageButton(int pageNumber)
        {
            bool isActive = (pageNumber == _currentPage);
            
            Button btn = new Button
            {
                Text = pageNumber.ToString(),
                Size = new Size(32, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, isActive ? FontStyle.Bold : FontStyle.Regular),
                Cursor = isActive ? Cursors.Default : Cursors.Hand,
                Margin = new Padding(2),
                Padding = new Padding(0)
            };
            btn.FlatAppearance.BorderSize = 0;
            
            if (isActive)
            {
                btn.BackColor = AppColors.Primary;
                btn.ForeColor = Color.White;
            }
            else
            {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = AppColors.TextPrimary;
                btn.FlatAppearance.MouseOverBackColor = AppColors.SurfaceHover;
                btn.Click += (s, e) => FirePageChanged(pageNumber);
            }

            return btn;
        }

        private Control CreateNavigateButton(string text, bool isEnabled, Action onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(32, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                Cursor = isEnabled ? Cursors.Hand : Cursors.Default,
                Margin = new Padding(2),
                Padding = new Padding(0),
                BackColor = isEnabled ? Color.Transparent : Color.Transparent,
                ForeColor = isEnabled ? AppColors.TextPrimary : AppColors.TextDisabled
            };
            btn.FlatAppearance.BorderSize = 0;
            
            if (isEnabled)
            {
                btn.FlatAppearance.MouseOverBackColor = AppColors.SurfaceHover;
                btn.Click += (s, e) => onClick();
            }
            else
            {
                btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            }

            return btn;
        }

        private Control CreateEllipsis()
        {
            return new Label
            {
                Text = "...",
                Size = new Size(20, 32),
                TextAlign = ContentAlignment.BottomCenter,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = AppColors.TextSecondary,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 0, 4)
            };
        }
    }
}
