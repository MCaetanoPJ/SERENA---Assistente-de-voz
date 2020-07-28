/*
 * Created by SharpDevelop.
 * User: Omnia
 * Date: 18/02/2020
 * Time: 09:53
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Serena_5._.View
{
	/// <summary>
	/// Description of Tela_Inicio.
	/// </summary>
	public partial class Tela_Inicio : Form
	{
		public delegate void NovoDelegate(string item);//Delegate desse Form
		
		Controller.Reconhecimento_Voz voz = new Controller.Reconhecimento_Voz();
		
		public Tela_Inicio()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			voz.Iniciar_Reconhecimento();
			
			//
			// TODO: Add constructor code after the InitializeComponent() call.
			//
		}
		
		void Tela_InicioLoad(object sender, EventArgs e)
		{
			//posição da tela ao iniciar
			StartPosition = FormStartPosition.Manual;
			Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - Width, 
            Screen.PrimaryScreen.WorkingArea.Height - Height);
			
			LabelPadrao();
			Model.OBJ_Configuracoes d = new Model.OBJ_Configuracoes();
			d.Consultar_Configuracoes();
			voz.Emulador_de_Voz("Bem Vindo "+d.getNomeUsuario()+". como posso te ajudar");
			
			//voz.RefAdicionaItens = new NovoDelegate(this.Atualiza_label_SerenaDisse);//Passa o valor recebido ao metodo
		}
		private void LabelPadrao()
		{
			LBL_SerenaDisse.Text = "-";
			LBL_Status_Sistema.Text = "-";
			LBL_VoceDisse.Text = "-";
		}
		void BTN_ConfiguracoesClick(object sender, EventArgs e)
		{
			View.Config_Voz c = new View.Config_Voz();
			c.Show();
		}
		void BTN_CriarComandosClick(object sender, EventArgs e)
		{
			View.Criador_Comandos c = new View.Criador_Comandos();
			c.Show();
		}
		void BTN_ExibirComandosClick(object sender, EventArgs e)
		{
			View.Exibir_Comandos c = new View.Exibir_Comandos();
			c.Show();
		}
		void BTN_ImportarComandosClick(object sender, EventArgs e)
		{
			View.Importar_Comandos c = new View.Importar_Comandos();
			c.Show();
		}
		public void Atualiza_label_VoceDisse(string VoceDisse)
		{
			LBL_VoceDisse.Text = VoceDisse;
			MessageBox.Show("Você Disse: "+VoceDisse);			
		}
		public void Atualiza_label_StatusSistema(string StatusSistema)
		{
			LBL_Status_Sistema.Text = StatusSistema;
			MessageBox.Show("Status do sistema: "+StatusSistema);
		}
		public void Atualiza_label_SerenaDisse(string SerenaDisse)
		{
			LBL_SerenaDisse.Text = SerenaDisse;
			MessageBox.Show("Serena Disse: "+SerenaDisse);
		}
	}
}
