/*
 * Created by SharpDevelop.
 * User: Omnia
 * Date: 11/03/2020
 * Time: 17:05
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace Serena_5._.Model
{
	/// <summary>
	/// Description of Conexao_Ditado.
	/// </summary>
	public class Conexao_Ditado
	{
		private string diretorio = "DataBase-Ditado";//Nome da pasta onde o BD está
		public SQLiteConnection Conexao_ditado = new SQLiteConnection("Data Source=DataBase-Ditado/Dicionario.db");//Cria e Acessa o Banco de Dados
		public Conexao_Ditado()
		{
			CriarTabelas();
		}
		
		private void CriarDiretorio()
		{
			try{
				if (!Directory.Exists(diretorio))
					{
						Directory.CreateDirectory(diretorio);//Cria o Diretorio
					}
			}
			catch(Exception ex){
				MessageBox.Show("Houve um erro ao criar o Diretório: "+diretorio+" \nMotivo: "+ex.Message);
			}
		}
		public void Conectar()
		{
			try{
				Conexao_ditado.Open();
			}
			catch(Exception Erro_Conectar){
				MessageBox.Show("Erro ao Abrir a Conexão: \n"+Erro_Conectar.Message+"\n Status da Conexao: "+Conexao_ditado.State);
			}
		}
		public void Desconectar()
		{
			try{
				Conexao_ditado.Close();
			}
			catch(Exception Erro_Desconectar){
				MessageBox.Show("Erro ao Fechar a Conexão: \n"+Erro_Desconectar.Message+"\n Status da Conexao: "+Conexao_ditado.State);
			}
		}
		private void CriarTabelas()
		{
			CriarDiretorio();
			try
            {
				Conectar();
                using (var commando = Conexao_ditado.CreateCommand())
                {
                    commando.CommandText = "CREATE TABLE IF NOT EXISTS Dicionario(" +
                    	"Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, " +
                    	"Palavra TEXT)";
                    commando.ExecuteNonQuery();
                    Desconectar();  
                }
            }
            catch(Exception ex){
				MessageBox.Show("Houve um erro ao criar a Tabela dentro do Banco de Dados \nMotivo: "+ex.Message);
			}
		}
	}
}
