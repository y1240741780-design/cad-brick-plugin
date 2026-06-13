using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Fanben.BrickPlugin.Core.Models;

namespace Fanben.BrickPlugin.UI.Palettes
{
    /// <summary>
    /// 排砖主面板 — 所有参数设置
    /// </summary>
    public partial class MainPalette : UserControl
    {
        // 控件声明
        private GroupBox _gbTileType;
        private RadioButton _rbWall, _rbFloor;
        private GroupBox _gbSpec;
        private ComboBox _cbSpecPreset;
        private TextBox _tbLength, _tbWidth;
        private Label _lblLength, _lblWidth;
        private GroupBox _gbJoint;
        private NumericUpDown _nudHorizontalJoint, _nudVerticalJoint;
        private GroupBox _gbPattern;
        private ComboBox _cbPattern;
        private GroupBox _gbStartPoint;
        private RadioButton _rbCenter, _rbCorner, _rbManual;
        private ComboBox _cbCornerDirection;
        private GroupBox _gbCut;
        private NumericUpDown _nudMinCutRatio;
        private CheckBox _chkPreferFullTile, _chkConfirmBeforeCut;
        private GroupBox _gbAvoidance;
        private CheckBox _chkAutoDetectOpenings;
        private GroupBox _gbPresentation;
        private CheckBox _chkOutline, _chkBlock, _chkHatch;
        private Button _btnStart;

        /// <summary>
        /// 面板暴露的排砖请求
        /// </summary>
        public LayoutRequest BuildRequest()
        {
            return new LayoutRequest
            {
                BrickSpec = GetSelectedSpec(),
                JointSetting = new JointSetting
                {
                    HorizontalJoint = (double)_nudHorizontalJoint.Value,
                    VerticalJoint = (double)_nudVerticalJoint.Value
                },
                Pattern = (LayoutPattern)_cbPattern.SelectedIndex,
                StartPointMode = GetStartPointMode(),
                CornerDirection = (CornerDirection)_cbCornerDirection.SelectedIndex,
                CutMode = GetCutMode(),
                MinCutRatio = (double)_nudMinCutRatio.Value,
                AvoidanceMode = _chkAutoDetectOpenings.Checked
                    ? AvoidanceMode.AutoDetect : AvoidanceMode.None,
                Presentation = GetPresentation()
            };
        }

        public MainPalette()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(280, 720);
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.Padding = new Padding(10);
            this.AutoScroll = true;

            int y = 10;
            int controlWidth = 240;

            // === 排砖类型 ===
            _gbTileType = new GroupBox
            {
                Text = "排砖类型", Location = new Point(10, y),
                Size = new Size(controlWidth, 50)
            };
            _rbWall = new RadioButton { Text = "墙砖", Location = new Point(10, 20), Checked = true };
            _rbFloor = new RadioButton { Text = "地砖", Location = new Point(100, 20) };
            _gbTileType.Controls.AddRange(new Control[] { _rbWall, _rbFloor });
            this.Controls.Add(_gbTileType);
            y += 60;

            // === 砖规格 ===
            _gbSpec = new GroupBox
            {
                Text = "砖规格", Location = new Point(10, y),
                Size = new Size(controlWidth, 100)
            };
            var lblPreset = new Label { Text = "预设规格:", Location = new Point(10, 20), AutoSize = true };
            _cbSpecPreset = new ComboBox
            {
                Location = new Point(80, 18), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var spec in BrickSpec.Presets)
                _cbSpecPreset.Items.Add(spec.Name);
            _cbSpecPreset.SelectedIndex = 0;
            _cbSpecPreset.SelectedIndexChanged += OnSpecPresetChanged;

            _lblLength = new Label { Text = "长(mm):", Location = new Point(10, 50), AutoSize = true };
            _tbLength = new TextBox { Location = new Point(80, 47), Width = 70, Text = "600" };
            _lblWidth = new Label { Text = "宽(mm):", Location = new Point(155, 50), AutoSize = true };
            _tbWidth = new TextBox { Location = new Point(200, 47), Width = 50, Text = "300" };

            _gbSpec.Controls.AddRange(new Control[] { lblPreset, _cbSpecPreset,
                _lblLength, _tbLength, _lblWidth, _tbWidth });
            this.Controls.Add(_gbSpec);
            y += 110;

            // === 灰缝 ===
            _gbJoint = new GroupBox
            {
                Text = "灰缝设置", Location = new Point(10, y),
                Size = new Size(controlWidth, 80)
            };
            var lblH = new Label { Text = "水平缝(mm):", Location = new Point(10, 22), AutoSize = true };
            _nudHorizontalJoint = new NumericUpDown
            {
                Location = new Point(88, 20), Width = 60, Minimum = 0, Maximum = 20,
                DecimalPlaces = 1, Value = 2, Increment = 0.5m
            };
            var lblV = new Label { Text = "垂直缝(mm):", Location = new Point(10, 50), AutoSize = true };
            _nudVerticalJoint = new NumericUpDown
            {
                Location = new Point(88, 48), Width = 60, Minimum = 0, Maximum = 20,
                DecimalPlaces = 1, Value = 2, Increment = 0.5m
            };
            _gbJoint.Controls.AddRange(new Control[] { lblH, _nudHorizontalJoint, lblV, _nudVerticalJoint });
            this.Controls.Add(_gbJoint);
            y += 90;

            // === 铺贴方式 ===
            _gbPattern = new GroupBox
            {
                Text = "铺贴方式", Location = new Point(10, y),
                Size = new Size(controlWidth, 50)
            };
            _cbPattern = new ComboBox
            {
                Location = new Point(10, 20), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cbPattern.Items.AddRange(new object[] { "直铺 / 通缝铺", "工字铺 / 错缝铺", "人字铺 (Herringbone)", "斜铺 (45°)" });
            _cbPattern.SelectedIndex = 0;
            _gbPattern.Controls.Add(_cbPattern);
            this.Controls.Add(_gbPattern);
            y += 60;

            // === 起铺点 ===
            _gbStartPoint = new GroupBox
            {
                Text = "起铺点", Location = new Point(10, y),
                Size = new Size(controlWidth, 100)
            };
            _rbCenter = new RadioButton { Text = "居中起铺", Location = new Point(10, 20), Checked = true };
            _rbCorner = new RadioButton { Text = "角落起铺", Location = new Point(10, 42) };
            _rbManual = new RadioButton { Text = "手动指定", Location = new Point(10, 64) };
            _cbCornerDirection = new ComboBox
            {
                Location = new Point(100, 40), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cbCornerDirection.Items.AddRange(new object[] { "左下角", "右下角", "左上角", "右上角" });
            _cbCornerDirection.SelectedIndex = 0;

            _rbCorner.CheckedChanged += (s, e) =>
            { _cbCornerDirection.Enabled = _rbCorner.Checked; };
            _cbCornerDirection.Enabled = false;

            _gbStartPoint.Controls.AddRange(new Control[] {
                _rbCenter, _rbCorner, _rbManual, _cbCornerDirection });
            this.Controls.Add(_gbStartPoint);
            y += 110;

            // === 切割设置 ===
            _gbCut = new GroupBox
            {
                Text = "切割设置", Location = new Point(10, y),
                Size = new Size(controlWidth, 100)
            };
            var lblMin = new Label { Text = "最小切割比例:", Location = new Point(10, 22), AutoSize = true };
            _nudMinCutRatio = new NumericUpDown
            {
                Location = new Point(100, 20), Width = 60, Minimum = 0.1m, Maximum = 1.0m,
                DecimalPlaces = 2, Value = 0.33m, Increment = 0.05m
            };
            _chkPreferFullTile = new CheckBox
            {
                Text = "优先整砖", Location = new Point(10, 48), Checked = true, AutoSize = true
            };
            _chkConfirmBeforeCut = new CheckBox
            {
                Text = "切割前确认", Location = new Point(10, 70), Checked = true, AutoSize = true
            };
            _gbCut.Controls.AddRange(new Control[] { lblMin, _nudMinCutRatio,
                _chkPreferFullTile, _chkConfirmBeforeCut });
            this.Controls.Add(_gbCut);
            y += 110;

            // === 避让设置 ===
            _gbAvoidance = new GroupBox
            {
                Text = "避让设置", Location = new Point(10, y),
                Size = new Size(controlWidth, 50)
            };
            _chkAutoDetectOpenings = new CheckBox
            {
                Text = "自动检测门窗洞口", Location = new Point(10, 20), Checked = true, AutoSize = true
            };
            _gbAvoidance.Controls.Add(_chkAutoDetectOpenings);
            this.Controls.Add(_gbAvoidance);
            y += 60;

            // === 呈现方式 ===
            _gbPresentation = new GroupBox
            {
                Text = "呈现方式", Location = new Point(10, y),
                Size = new Size(controlWidth, 85)
            };
            _chkOutline = new CheckBox
            {
                Text = "轮廓线", Location = new Point(10, 18), Checked = true, AutoSize = true
            };
            _chkBlock = new CheckBox
            {
                Text = "实体块", Location = new Point(10, 38), Checked = true, AutoSize = true
            };
            _chkHatch = new CheckBox
            {
                Text = "填充图案", Location = new Point(10, 58), Checked = true, AutoSize = true
            };
            _gbPresentation.Controls.AddRange(new Control[] { _chkOutline, _chkBlock, _chkHatch });
            this.Controls.Add(_gbPresentation);
            y += 95;

            // === 开始按钮 ===
            _btnStart = new Button
            {
                Text = "▶ 开始排砖",
                Location = new Point(10, y),
                Size = new Size(controlWidth, 40),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            _btnStart.FlatAppearance.BorderSize = 0;
            _btnStart.Click += OnStartClick;
            this.Controls.Add(_btnStart);
        }

        // ============ 事件处理 ============

        private void OnSpecPresetChanged(object sender, EventArgs e)
        {
            string name = _cbSpecPreset.SelectedItem?.ToString();
            var spec = BrickSpec.Presets.FirstOrDefault(s => s.Name == name);
            if (spec != null)
            {
                _tbLength.Text = spec.Length.ToString("F0");
                _tbWidth.Text = spec.Width.ToString("F0");
            }
        }

        /// <summary>
        /// 开始排砖按钮点击事件
        /// </summary>
        public event EventHandler StartClicked;

        private void OnStartClick(object sender, EventArgs e)
        {
            // 验证输入
            if (!double.TryParse(_tbLength.Text, out double len) || len <= 0 ||
                !double.TryParse(_tbWidth.Text, out double wid) || wid <= 0)
            {
                MessageBox.Show("请输入有效的砖规格尺寸", "输入错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            StartClicked?.Invoke(this, EventArgs.Empty);
        }

        // ============ 辅助方法 ============

        private BrickSpec GetSelectedSpec()
        {
            string name = _cbSpecPreset.SelectedItem?.ToString() ?? "自定义";
            double.TryParse(_tbLength.Text, out double len);
            double.TryParse(_tbWidth.Text, out double wid);
            return new BrickSpec { Name = name, Length = len, Width = wid, Thickness = 10 };
        }

        private StartPointMode GetStartPointMode()
        {
            if (_rbCenter.Checked) return StartPointMode.Center;
            if (_rbCorner.Checked) return StartPointMode.Corner;
            return StartPointMode.Manual;
        }

        private CutConstraintMode GetCutMode()
        {
            if (_chkPreferFullTile.Checked && _chkConfirmBeforeCut.Checked)
                return CutConstraintMode.Comprehensive;
            if (_chkPreferFullTile.Checked)
                return CutConstraintMode.PreferFullTile;
            return CutConstraintMode.MinSize;
        }

        private PresentationStyle GetPresentation()
        {
            PresentationStyle style = PresentationStyle.None;
            if (_chkOutline.Checked) style |= PresentationStyle.Outline;
            if (_chkBlock.Checked) style |= PresentationStyle.Block;
            if (_chkHatch.Checked) style |= PresentationStyle.Hatch;
            return style;
        }
    }
}
