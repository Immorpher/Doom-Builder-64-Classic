using System;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.AutomapMode
{
	public partial class MenusForm : UserControl
	{
		public event EventHandler OnShowHiddenLinesChanged;
		public event EventHandler OnShowSecretSectorsChanged;
		internal event EventHandler OnTypeChanged;

		public bool ShowHiddenLines { get { return showhiddenlines.Checked; } set { showhiddenlines.Checked = value; } }
		public bool ShowSecretSectors { get { return showsecretsectors.Checked; } set { showsecretsectors.Checked = value; } }
		internal AutomapMode.AutomapType Type { get { return (AutomapMode.AutomapType)automaptype.SelectedIndex; } set { automaptype.SelectedIndex = (int)value; } }

		public MenusForm()
		{
			InitializeComponent();
		}

		public void Register()
		{
			General.Interface.AddButton(showhiddenlines);
			General.Interface.AddButton(showsecretsectors);
			General.Interface.AddButton(typeseparator);
			General.Interface.AddButton(typelabel);
			General.Interface.AddButton(automaptype);
		}

		public void Unregister()
		{
			General.Interface.RemoveButton(automaptype);
			General.Interface.RemoveButton(typelabel);
			General.Interface.RemoveButton(typeseparator);
			General.Interface.RemoveButton(showsecretsectors);
			General.Interface.RemoveButton(showhiddenlines);
		}

		private void showhiddenlines_CheckedChanged(object sender, EventArgs e)
		{
			if(OnShowHiddenLinesChanged != null) OnShowHiddenLinesChanged(showhiddenlines.Checked, EventArgs.Empty);
		}

		private void showsecretsectors_CheckedChanged(object sender, EventArgs e)
		{
			if(OnShowSecretSectorsChanged != null) OnShowSecretSectorsChanged(showsecretsectors.Checked, EventArgs.Empty);
		}

		private void automaptype_SelectedIndexChanged(object sender, EventArgs e)
		{
			if(OnTypeChanged != null) OnTypeChanged(automaptype.SelectedIndex, EventArgs.Empty);
		}
	}
}
