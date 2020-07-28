/*
 * Created by SharpDevelop.
 * User: Omnia
 * Date: 19/02/2020
 * Time: 14:40
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Windows.Forms;

//usado pelo reconhecimento
using Microsoft.Speech.Recognition;
using Microsoft.Speech.Synthesis;
using System.Globalization;

namespace Serena_5._.Controller
{
	/// <summary>
	/// Description of Reconhecimento_Voz.
	/// </summary>
	public class Reconhecimento_Voz
	{
		public delegate void SintetizarVoz(string voice_to_text);
		
		//Variavel usada para abrir os programas
		static Process Processo;
        static SpeechRecognitionEngine reconhecedor;
        public SpeechSynthesizer Sintetiza_Voz = new SpeechSynthesizer();
        
		public Reconhecimento_Voz()
		{
			
		}
		
		public void Iniciar_Reconhecimento()
        {
			Model.Conexao_BD c = new Model.Conexao_BD();
			//c.CriarBancoDeDados();//Cria caso não exista
			
			Controles_do_sintetizador_de_voz();//Inicializa a configuração de voz salva no BD
			Carregar_Cultura();//Inicializa a cultura pt-BR
			Carregar_Gramatica_interna();// inicialização da gramatica Interna
			Carregar_Gramatica_SQL();//inicialização da gramatica SQL
			Carregar_Entrada_Saida_de_Voz();//Defini a entrada e saida da voz usadas
        }
		
		/*---------------------Reconhecimento de voz---------------------------*/
		public void Controles_do_sintetizador_de_voz()
		{
			Model.OBJ_Configuracoes c = new Model.OBJ_Configuracoes();
			c.Consultar_Configuracoes();	
			try{
				Sintetiza_Voz.Volume = Convert.ToInt32(c.getVolumeVoz()); // controla volume de saida
				Sintetiza_Voz.Rate = Convert.ToInt32(c.getVelocidadeVoz()); // velocidade de fala
			}
			catch(Exception ex){
				MessageBox.Show("Houve um erro ao definir o volume ou a fala do sintetizador de voz \nMotivo: " + ex.Message);
			}
		}
		private void Carregar_Cultura()
		{
			try{
				CultureInfo ci = new CultureInfo("pt-BR");// Idioma utilizado
                reconhecedor = new SpeechRecognitionEngine(ci);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Houve um erro durante o uso da cultura pt-BR escolhida \nMotivo: " + ex.Message);
            }
		}
		private void Carregar_Gramatica_interna()
		{
			Model.Pronuncia_Comandos p = new Model.Pronuncia_Comandos();
			try{
				//Ler as pronuncias internas e adiciona em uma lista
				var lista_pronuncias = new Choices();
				foreach (string element in p.Comandos_Internos)
				{
					lista_pronuncias.Add(element);
				}
				
				//Construindo a gramatica com a lista de pronuncias
				GrammarBuilder gramatica = new GrammarBuilder();
				gramatica.Append(lista_pronuncias);
				
				//passando a gramatica criada para a biblioteca
				Grammar g = new Grammar(gramatica);
				g.Name = "Gramatica_Interna";
				reconhecedor.RequestRecognizerUpdate();
				reconhecedor.LoadGrammarAsync(g);
			}
            catch (Exception ex)
            {
                MessageBox.Show("Houve um erro ao criar ao carregar ou criar a Gramatica de comandos internos do sistema \nMotivo: " + ex.Message);
            }
            try{
            	//reconhecedor.SpeechDetected += reconhecedor_SpeechDetected;//Detecção de Voz
                reconhecedor.SpeechRecognized += Executa_Comandos_Internos;//Executa ações com comando de voz
               	//reconhecedor.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(Executar_Comandos.Executa_Comandos_Internos);
            }
            catch(Exception ex){
            	MessageBox.Show("Erro encontrado ao detectar a voz do usuário ou executar os comandos \nMotivo: " + ex.Message);
            }
		}
		private void Carregar_Gramatica_Ditado()
		{
			var lista_pronuncias = new Choices();
			try
            {
				DataTable Tabela = new DataTable();
	        	string Comando_SQL = "Select Palavra from Dicionario";
	        	Model.Conexao_Ditado c = new Model.Conexao_Ditado();
	        	c.Conectar();
	        	SQLiteCommand Comando = new SQLiteCommand(Comando_SQL,c.Conexao_ditado);//instancio o metodo SqlliteCommand
	        	SQLiteDataReader sdr = Comando.ExecuteReader();//Lê o resultado de um comando sql
	        	
	        	while (sdr.Read()){
					//Faz a leitura de toda a coluna 'Pronuncia'
	        		lista_pronuncias.Add(sdr["Palavra"].ToString());
	        	}
				c.Desconectar();
				
				//Construindo a gramatica com a lista de pronuncias
				var gramatica = new GrammarBuilder();
				gramatica.Append(lista_pronuncias);
				
				//passando a gramatica criada para a biblioteca
				var g_sql = new Grammar(gramatica);
				g_sql.Name = "Gramatica_SQL_Ditado";
				reconhecedor.RequestRecognizerUpdate();//Pausa o reconhecimento e atualiza a gramatica
				reconhecedor.LoadGrammarAsync(g_sql);
				
				MessageBox.Show("Modo Ditado Ativado! \n\n Para desativar diga: Desativar modo ditado");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Houve um erro ao criar ou carregar a Gramatica de Ditado SQL \nMotivo: " + ex.Message);
            }
            try{
            	//reconhecedor.SpeechDetected += reconhecedor_SpeechDetected;//Detecção de Voz
                reconhecedor.SpeechRecognized += Executa_Comandos_Ditado;//Executa ações com comando de voz
            }
            catch(Exception ex){
            	MessageBox.Show("Erro encontrado ao detectar a voz do usuário ou executar os comandos do banco de dados\nMotivo: " + ex.Message);
            }
		}
		public void Carregar_Gramatica_SQL()
		{
			var lista_pronuncias = new Choices();
			try
            {
				DataTable Tabela = new DataTable();
	        	string Comando_SQL = "Select Id,Pronuncia,Instrucao,RespostaEsperada,IsCommand,Executar1,Executar2,TipoComando from controlecomando";
	        	Model.Conexao_BD c = new Model.Conexao_BD();
	        	c.Conexao.Open();
	        	SQLiteCommand Comando = new SQLiteCommand(Comando_SQL,c.Conexao);//instancio o metodo SqlliteCommand
	        	SQLiteDataReader sdr = Comando.ExecuteReader();//Lê o resultado de um comando sql
	        	
	        	while (sdr.Read()){
					//Faz a leitura de toda a coluna 'Pronuncia'
	        		lista_pronuncias.Add(sdr["Pronuncia"].ToString());
	        	}
				c.Conexao.Close();
				
				//Construindo a gramatica com a lista de pronuncias
				var gramatica = new GrammarBuilder();
				gramatica.Append(lista_pronuncias);
				
				//passando a gramatica criada para a biblioteca
				var g_sql = new Grammar(gramatica);
				g_sql.Name = "Gramatica_SQL";
				reconhecedor.RequestRecognizerUpdate();//Pausa o reconhecimento e atualiza a gramatica
				reconhecedor.LoadGrammarAsync(g_sql);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Houve um erro ao criar ou carregar a Gramatica de comandos SQL do sistema \nMotivo: " + ex.Message);
            }
            try{
            	//reconhecedor.SpeechDetected += reconhecedor_SpeechDetected;//Detecção de Voz
                reconhecedor.SpeechRecognized += Executa_Comandos_Sqlite;//Executa ações com comando de voz
            }
            catch(Exception ex){
            	MessageBox.Show("Erro encontrado ao detectar a voz do usuário ou executar os comandos do banco de dados\nMotivo: " + ex.Message);
            }
		}
		private void Carregar_Entrada_Saida_de_Voz()
		{
			try{
				reconhecedor.SetInputToDefaultAudioDevice(); // microfone padrao
	            Sintetiza_Voz.SetOutputToDefaultAudioDevice(); // auto falante padrao
	            
	            try{
	            reconhecedor.RecognizeAsync(RecognizeMode.Multiple); // multiplo reconhecimento
	            }
	            catch(Exception ex){
	            	MessageBox.Show("Não foi possível fazer a leitura da gramática SQL Comandos, Verifique se ela está vazia ou importe outro arquivo com comandos \n\nMotivo: "+ex.Message);
	            }
	            
			}
            catch(Exception ex)
            {
            	MessageBox.Show("É necessário conectar um microfone antes de usar, \nMotivo: " + ex.Message, "Nenhum microfone encontrado!!");
            }

		}
		
		private void Remover_Gramatica_Ditado()
		{
			foreach (Grammar g in reconhecedor.Grammars)  
	        {  
				if(g.Name == "Gramatica_SQL_Ditado")
				{
					reconhecedor.UnloadGrammar(g);
					reconhecedor.RequestRecognizerUpdate();
				}
	        } 
		}
		private void Exibir_Gramaticas_Carregadas()
		{
			foreach (Grammar g in reconhecedor.Grammars)  
	        {  
				MessageBox.Show("Gramáticas Carregadas: "+g.Name);
	        } 
		}
		
		/*---------------------Ações do reconhecimento de voz---------------------------*/
		private void reconhecedor_SpeechDetected(object sender, SpeechDetectedEventArgs e)
		{
			//throw new NotImplementedException();
		}
        private void Executa_Comandos_Internos(object sender, SpeechRecognizedEventArgs e){
			if(e.Result == null) return;
				foreach (RecognizedPhrase frase in e.Result.Alternates){
			    	if(frase.Confidence >= 0.6){
		            	string speech = e.Result.Text;
		            	try{
		            		switch(speech){
		            			case "Selecionar Tudo":
		            				//Método que lê o texto que estiver na área de tranferência
		            				SendKeys.Send("^a");
									Emulador_de_Voz("Tudo Selecionado");
		            		break;
		            			case "Copiar":
		            				//Método que lê o texto que estiver na área de tranferência
		            				Emulador_de_Voz("Copiando");
		            				SendKeys.Send("^c");
		            		break;
		            			case "Fechar Tela":
		            				Emulador_de_Voz("Fechando a tela atual");
		            				SendKeys.Send("%{F4}");
		            		break;
		            			case "Colar":
		            				//Método que lê o texto que estiver na área de tranferência
		            				SendKeys.Send("^v");
									Emulador_de_Voz("Colando");
		            		break;
		            			case "Leia esse texto":
		            				//Método que lê o texto que estiver na área de tranferência
		            				SendKeys.Send("^c");
									Emulador_de_Voz(Clipboard.GetText());
		            		break;
		            			case "Pesquisa no Google":
		            				//Método que copia o que o usuário selecionou e pesquisa a informação no Google
		            				SendKeys.Send("^c");
									Process.Start("https://www.google.com/search?q="+ Clipboard.GetText());
									Emulador_de_Voz("Procurando."+Clipboard.GetText()+".No google");
		            		break;
		            			case "Pesquisa no wikipedia":
		            				//Método que copia o que o usuário selecionou e pesquisa a informação no Google
		            				Emulador_de_Voz("Procurando."+Clipboard.GetText()+".No úikipedía");
		            				SendKeys.Send("^c");
									Process.Start("https://pt.wikipedia.org/wiki/"+ Clipboard.GetText());
		            		break;
		            			case "Pesquisa Imagens":
		            				//Método que copia o que o usuário selecionou e pesquisa a informação no Google
		            				Emulador_de_Voz("Procurando imagens sobre."+Clipboard.GetText());
		            				SendKeys.Send("^c");
									Process.Start("https://www.google.com/search?q="+ Clipboard.GetText()+"&tbm=isch");
		            		break;
		            			case "Pesquisa Videos":
		            				//Método que copia o que o usuário selecionou e pesquisa a informação no Google
		            				Emulador_de_Voz("Procurando Vídeos sobre."+Clipboard.GetText());
		            				SendKeys.Send("^c");
									Process.Start("https://www.google.com/search?q="+ Clipboard.GetText()+"&tbm=vid");
		            		break;
		            			case "Pesquisa Noticias":
		            				//Método que copia o que o usuário selecionou e pesquisa a informação no Google
		            				Emulador_de_Voz("Procurando Notícias sobre."+Clipboard.GetText());
		            				SendKeys.Send("^c");
									Process.Start("https://www.google.com/search?q="+ Clipboard.GetText()+"&tbm=nws");
		            		break;
		            			case "Pesquisa no Youtube":
		            				//Método que copia o que o usuário selecionou e pesquisa a informação no Google
		            				Emulador_de_Voz("Procurando. "+Clipboard.GetText()+"No youtube");
		            				SendKeys.Send("^c");
									Process.Start("https://www.youtube.com/results?search_query="+ Clipboard.GetText());
		            		break;
		            			case "Que horas são":
		            				Emulador_de_Voz("agora são exatamente,"+DateTime.Now.ToString("hh:mm"));
		            		break;
		            			case "Quem é você":
                					Emulador_de_Voz("Pode me chamar de SERENA, sou uma interfeice de auxílio ao usuário");
                			break;
                				case "Para de Falar":
	                				//Encerra todos os processos que usem a voz
	                				Encerrar_Voz();
	                				Emulador_de_Voz("Desculpe");
                			break;
                				case "Abrir lista de comandos":
                					Emulador_de_Voz("Exibindo Lista de comandos");
                					View.Exibir_Comandos abrir_comandos = new View.Exibir_Comandos();
	                				abrir_comandos.Show();
                			break;
                				case "Fechar lista de comandos":
                					Emulador_de_Voz("Fechando Lista de comandos");
                					if (Application.OpenForms["Config_Voz"] != null)
      									Application.OpenForms["Config_Voz"].Close();
                			break;
                				case "Abrir Criador de Comandos":
                					Emulador_de_Voz("Abrindo tela para criar comando");
                					View.Criador_Comandos Criar_Comando = new View.Criador_Comandos();
	                				Criar_Comando.Show();
                			break;
                				case "Fechar Criador de Comandos":
                					Emulador_de_Voz("Fechando tela");
					                if (Application.OpenForms["Criador_Comandos"] != null)
      									Application.OpenForms["Criador_Comandos"].Close();
                			break;
                				case "Abrir Configurações de voz":
                					Emulador_de_Voz("Abrindo Configurações de voz");
                					View.Config_Voz Abrir_configuracoes = new View.Config_Voz();
	                				Abrir_configuracoes.Show();
                			break;
                				case "Fechar Configurações de voz":
                					Emulador_de_Voz("Fechando Configurações de voz");
                					if (Application.OpenForms["Config_Voz"] != null)
      									Application.OpenForms["Config_Voz"].Close();
                			break;
                				case "Ativar modo ditado":
                					Emulador_de_Voz("Carregando modo Ditado");
                					Carregar_Gramatica_Ditado();
                					MessageBox.Show("O Modo Ditado está sendo iniciado, aguarde!");
                			break;
                				case "Desativar modo ditado":
                					Emulador_de_Voz("Desativando modo Ditado");
                					Remover_Gramatica_Ditado();
                					
                			break;
		            			default:
		            		break;
		            		}
		            	}
		            	catch(Exception Erro){
		            		MessageBox.Show("Houve um erro com os comandos internos \n"+Erro.Message);
		            	}
					}
				}
		}
		private void Executa_Comandos_Sqlite(object sender, SpeechRecognizedEventArgs e){
			if(e.Result == null) return;
				foreach (RecognizedPhrase frase in e.Result.Alternates){
			    	if(frase.Confidence >= 0.6){
		            	string speech = e.Result.Text;
			            try{
		            		
		            		DataTable Tabela = new DataTable();
				        	string Comando_SQL = "Select Id,Pronuncia,Instrucao,RespostaEsperada,IsCommand,Executar1,Executar2,TipoComando from controlecomando";
				        	Model.Conexao_BD c = new Model.Conexao_BD();
				        	c.Conectar();
				        	SQLiteCommand Comando = new SQLiteCommand(Comando_SQL,c.Conexao);//instancio o metodo SqlliteCommand
				        	SQLiteDataReader sdr = Comando.ExecuteReader();//Lê o resultado de um comando sql

				        	while (sdr.Read())
							{
				                {				                    
				                    string Id = sdr["Id"].ToString();
					            	string Pronuncia = sdr["Pronuncia"].ToString();
					            	string Instrucao = sdr["Instrucao"].ToString();
					            	string RespostaEsperada = sdr["RespostaEsperada"].ToString();
					            	string IsCommand = sdr["IsCommand"].ToString();
					            	string Executar1 = sdr["Executar1"].ToString();
					            	string Executar2 = sdr["Executar2"].ToString();
					            	string TipoComando = sdr["TipoComando"].ToString();
			            		
				            	if (Pronuncia == speech)
				                    {
				                    	if(IsCommand == "Sim")
				                    	{
				                    		switch (TipoComando)
				                    		{
				                    		case "CMD":
				                    			try
				                    			{
				                    				Emulador_de_Voz(RespostaEsperada);
				                    				if(Executar1 == "")
				                    				{
				                    					Process.Start("\""+Executar2+"\"");
				                    					Encerrar_Voz();
				                    				}
				                    				else if(Executar2 == "")
				                    				{
				                    					Process.Start("\""+Executar1+"\"");
				                    					Encerrar_Voz();
				                    				}
				                    				else
				                    				{
				                    					Process.Start("\""+Executar1+"\"","\""+Executar2+"\"");//scape para poder usar as aspas dentro de outras aspas
				                    					Encerrar_Voz();
				                    				}
				                    			}
				                    			catch(Exception erro)
				                    			{
				                    			//Emulador_de_Voz("Encontrei um erro nesse comando, por padrão será deletado da base de dados");
				                    			MessageBox.Show(DateTime.Now+"\n"+"Detalhes: "+erro.Message);
				                    			Encerrar_Voz();
				                    			}
				                    		break;
				                    		case "Tecla":
				                    			try
				                    			{
				                    				Emulador_de_Voz(RespostaEsperada);
				                    				SendKeys.Send(Executar1);
				                    				Encerrar_Voz();
				                    			}
				                    			catch(Exception erro)
				                    			{
					                    			//Emulador_de_Voz("Encontrei um erro ao usar essa teclas, por padrão será deletado da base de dados");
					                    			MessageBox.Show(DateTime.Now+"\n"+"Detalhes: "+erro.Message);
					                    			Encerrar_Voz();
				                    			}
				                    		break;
				                    		case "Site":
				                    			try
				                    			{
				                    				Emulador_de_Voz(RespostaEsperada);
				                    				Process.Start("\""+Executar1+"\"");
				                    				Encerrar_Voz();
				                    			}
				                    			catch(Exception erro)
				                    			{
					                    			//Emulador_de_Voz("Este não é um link válido, por padrão será deletado da base de dados");
					                    			MessageBox.Show(DateTime.Now+"\n"+"Detalhes: "+erro.Message);
					                    			Encerrar_Voz();
				                    			}
				                    		break;
				                    		case "Programa":
				                    			try
				                    			{
				                    				Emulador_de_Voz(RespostaEsperada);
				                    				//Process.Start("\""+Executar1+"\"");
				                    				
				                    				//Função em fase de teste
				                    				Processo = new Process();
									        		//Caminho do programa está na variavel Executar1
									        		Processo.StartInfo.FileName = Executar1;
									        		Processo.Start();
									        		
				                    				Encerrar_Voz();
				                    			}
				                    			catch(Exception erro)
				                    			{
					                    			Emulador_de_Voz("O Endereço do programa não foi encontrado");
					                    			MessageBox.Show(DateTime.Now+"\n"+"Detalhes: "+erro.Message);
					                    			Encerrar_Voz();
				                    			}
				                    		break;
				                    		default:
					                    		Emulador_de_Voz("Não conheço o tipo de comando da instrução,"+speech+"");
				                    		break;
				                    		}
				                    	}
				                    	else{
				                    		//Caso não seja um comando, escrever a instrução na tela
					                    	SendKeys.Send(Instrucao+" ");
					                    	Encerrar_Voz();
				                    	}
				                    }
				                }
			            	}
							sdr.Close();
							c.Desconectar();
			            }
		            	catch (Exception ex){
		                	MessageBox.Show("Você perguntou: "+speech+"\nDetalhes: " +ex.Message);
		            	}
		        	}
			}
		}
		private void Executa_Comandos_Ditado(object sender, SpeechRecognizedEventArgs e){
			if(e.Result == null) return;
				foreach (RecognizedPhrase frase in e.Result.Alternates){
			    	if(frase.Confidence >= 0.6){
		            	string speech = e.Result.Text;
		            	try
            			{
		            		MessageBox.Show("Você disse: "+speech);
            				//SendKeys.Send(speech);
            			}
            			catch(Exception erro)
            			{
                			//Emulador_de_Voz("Encontrei um erro ao usar essa teclas, por padrão será deletado da base de dados");
                			MessageBox.Show("Houve um erro ao usar o Ditado por voz\n"+"Detalhes: "+erro.Message);
            			}	
				}
			}
		}
		
		/*---------------------Sintetizador de voz---------------------------*/
		public void Emulador_de_Voz(string Texto_para_Voz)
		{
			//Controles_do_sintetizador_de_voz();
			try{
				Sintetiza_Voz.SpeakAsync(Texto_para_Voz);
				//MessageBox.Show("recebi: "+Texto_para_Voz);
			}
			catch(Exception ex){
				MessageBox.Show("Houve um erro ao sintetizar a voz, \nMotivo: "+ex.Message);
			}
		}
		public void Encerrar_Voz()
		{
			Sintetiza_Voz.SpeakAsyncCancelAll();
		}
		
	}
}
