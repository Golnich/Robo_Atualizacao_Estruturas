using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace AtualizacaoDeEstruturas.Service
{
    public class AtualizacaoEstruturasAppService : IAtualizacaoEstruturasAppService
    {
        public string AtualizaBanco(AtualizacaoDeEstruturasViewModel parametros)
        {
            //Criando variaveis
            var listaBancos = new List<dynamic>();
            var NovaStringConexao = ManipulandoStringConexao(parametros.String_Conexao, parametros.Banco_Principal);
            //Abrindo conexao com banco
            using (var dapper = new SqlConnection(parametros.String_Conexao))
            {
                listaBancos = dapper.Query(@"select name as Nome from sys.databases order by nome", commandType: CommandType.Text).ToList();
            }
            //Percorrendo lista de bancos
            foreach (var banco in listaBancos)
            {
                if (banco.Nome == parametros.Banco_Principal)
                {
                    //Abrindo conexao com banco (BASE)
                    using (var dapper = new SqlConnection(NovaStringConexao))
                    {
                        if (parametros.Estrutura == true)
                        {
                            //Listando tabelas do banco base (BASE)
                            var listaTabelas = new List<dynamic>();
                            listaTabelas = dapper.Query(@"SELECT distinct TABLE_NAME FROM INFORMATION_SCHEMA.COLUMNS", commandType: CommandType.Text).ToList();
                            //Percorrendo tabelas(BASE)
                            for (var i = 0; i < listaTabelas.Count; i++)
                            {
                                var estrutura = "";
                                //Listando estrutura da tabela(BASE)
                                var listaEstruturas = new List<dynamic>();
                                //var colunas_Existentes = new List<dynamic>();
                                string stringListaEstruturaTabela = string.Format(@"SELECT * FROM INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = '{0}'", listaTabelas[i].TABLE_NAME);
                                listaEstruturas = dapper.Query(stringListaEstruturaTabela, commandType: CommandType.Text).ToList();
                                var colunas_Existentes = new List<string>();
                                //Montando Estrutura da tabela(BASE)
                                for (var j = 0; j < listaEstruturas.Count; j++)
                                {

                                    var coluna = listaEstruturas[j].COLUMN_NAME;
                                    var tipo = listaEstruturas[j].DATA_TYPE;
                                    string tamanho = null;
                                    if (listaEstruturas[j].DATA_TYPE == "nvarchar" || listaEstruturas[j].DATA_TYPE == "varchar")
                                    {
                                        var resultado = listaEstruturas[j].CHARACTER_OCTET_LENGTH - listaEstruturas[j].CHARACTER_MAXIMUM_LENGTH;
                                        tamanho = string.Format("({0})", resultado == 0 ? "MAX" : Convert.ToString(resultado));
                                    }
                                    if (listaEstruturas[j].DATA_TYPE == "varbinary")
                                    {
                                        tamanho = string.Format("({0})", listaEstruturas[j].CHARACTER_OCTET_LENGTH == -1 ? "MAX" : listaEstruturas[j].CHARACTER_OCTET_LENGTH);
                                    }
                                    if (listaEstruturas[j].DATA_TYPE == "decimal")
                                    {
                                        tamanho = string.Format("({0},{1})", listaEstruturas[j].NUMERIC_PRECISION, listaEstruturas[j].NUMERIC_SCALE);
                                    }
                                    var permite_Nulo = listaEstruturas[j].IS_NULLABLE == "YES" ? "null" : "not null";
                                    if (estrutura != "")
                                    {
                                        estrutura = estrutura + "," + "\r\n";
                                    }
                                    estrutura = estrutura + coluna + " " + tipo + tamanho + " " + permite_Nulo;
                                    string adicionar = Convert.ToString(coluna);
                                    colunas_Existentes.Add(adicionar.ToUpper());
                                }
                                Criacao_De_Estruturas(parametros, estrutura, listaTabelas[i].TABLE_NAME, colunas_Existentes);
                            }

                        }

                        if (parametros.Procedures == true)
                        {
                            //Listando procedures do banco (BASE)
                            var listaProcedures = new List<dynamic>();
                            listaProcedures = dapper.Query(@"select name as Nome from sys.objects Where type = 'P'", commandType: CommandType.Text).ToList();
                            for (var i = 0; i < listaProcedures.Count; i++)
                            {
                                if (listaProcedures[i].Nome == "PR_SEEGER_LISTAR_PERMISSOES")
                                {
                                    var procedure_em_lista = new List<dynamic>();
                                    string comandoProcText = string.Format(@"sp_helptext {0}", listaProcedures[i].Nome);
                                    procedure_em_lista = dapper.Query(comandoProcText, commandType: CommandType.Text).ToList();
                                    string procedure_estrutura = "";
                                    for (var j = 0; j < procedure_em_lista.Count; j++)
                                    {
                                        if (procedure_estrutura == "")
                                        {
                                            procedure_estrutura = procedure_em_lista[j].Text + "\n";
                                        }
                                        else
                                        {
                                            procedure_estrutura = procedure_estrutura + procedure_em_lista[j].Text + "\n";
                                        }
                                    }
                                    Criacao_De_Procedures(parametros, procedure_estrutura, listaProcedures[i].Nome);
                                }
                                

                            }
                        }

                        if (parametros.Indices == true)
                        {
                            var listaGabaritoTabela = new List<dynamic>();
                            var listaIndices = new List<dynamic>();
                            listaIndices = dapper.Query(@"select * from sys.indexes", commandType: CommandType.Text).ToList();
                            if (listaIndices.Count > 0)
                            {
                                for (var i = 0; i < listaIndices.Count; i++)
                                {

                                    string comandoBuscaNomeTabela = string.Format(@"select name, object_id from sys.tables where object_id = {0}", listaIndices[i].object_id);
                                    var dadosTabela = dapper.Query(comandoBuscaNomeTabela, commandType: CommandType.Text).FirstOrDefault();

                                    if (dadosTabela != null && listaIndices[i].type_desc != "HEAP")
                                    {
                                        string ComandoBuscaIndex_Colum = string.Format(@"select * from sys.index_columns where object_id = {0}", listaIndices[i].object_id);
                                        List<dynamic> index_Colum = dapper.Query(ComandoBuscaIndex_Colum, commandType: CommandType.Text).ToList();
                                        foreach (var item in index_Colum)
                                        {
                                            string comandoBuscaColunas = string.Format(@"select object_id, name, column_id from sys.all_columns where object_id = {0} and column_id = {1}", listaIndices[i].object_id, item.index_column_id);
                                            var coluna = dapper.Query(comandoBuscaColunas, commandType: CommandType.Text).FirstOrDefault();
                                            Criacao_De_Indices(parametros, listaIndices[i], dadosTabela, coluna);
                                        }

                                    }
                                }
                            }
                        }

                        if (parametros.Chaves_Primarias == true)
                        {
                            var listaChaves = new List<dynamic>();
                            var comandoBuscaChaves = string.Format(@"select * from information_schema.table_constraints where CONSTRAINT_TYPE = 'PRIMARY KEY'");
                            listaChaves = dapper.Query(comandoBuscaChaves, commandType: CommandType.Text).ToList();

                            for (var i = 0; i < listaChaves.Count; i++)
                            {
                                string comandoDadosChave = string.Format(@"select * from information_schema.key_column_usage where CONSTRAINT_NAME = '{0}'", listaChaves[i].CONSTRAINT_NAME);
                                var dadosChavePrimaria = dapper.Query(comandoDadosChave, commandType: CommandType.Text).FirstOrDefault();

                                Criacao_De_Chaves_Primarias(parametros, dadosChavePrimaria);
                            }

                            var listaForeign = new List<dynamic>();
                            var comandoBuscaForeign = string.Format(@"select * from information_schema.table_constraints where CONSTRAINT_TYPE = 'FOREIGN KEY'");
                            listaForeign = dapper.Query(comandoBuscaForeign, commandType: CommandType.Text).ToList();

                            for (var i = 0; i < listaForeign.Count; i++)
                            {
                                string comandoDadosForeign = string.Format(@"select * FROM information_schema.key_column_usage where CONSTRAINT_NAME = '{0}'", listaForeign[i].CONSTRAINT_NAME);
                                var ForeignDados = dapper.Query(comandoDadosForeign, commandType: CommandType.Text).FirstOrDefault();
                                string comandoEstruturaCriacaoForeign = string.Format(@"select * from sys.foreign_keys where name = '{0}'", listaForeign[i].CONSTRAINT_NAME);
                                var EstruturaCriacaoForeign = dapper.Query(comandoEstruturaCriacaoForeign, commandType: CommandType.Text).FirstOrDefault();
                                string comandoIndexTabelaReferente = string.Format(@"select * from sys.indexes where object_id = {0} and index_id = {1}", EstruturaCriacaoForeign.referenced_object_id, EstruturaCriacaoForeign.key_index_id);
                                var IndexTabelaReferente = dapper.Query(comandoIndexTabelaReferente, commandType: CommandType.Text).FirstOrDefault();
                                string comandoPrimariKeyTabelaReferente = string.Format(@"select * FROM information_schema.key_column_usage where CONSTRAINT_NAME = '{0}'", IndexTabelaReferente.name);
                                var PrimariKeyTabelaReferente = dapper.Query(comandoPrimariKeyTabelaReferente, commandType: CommandType.Text).FirstOrDefault();

                                Criacao_De_Foreign(parametros, ForeignDados, EstruturaCriacaoForeign, PrimariKeyTabelaReferente);
                            }
                        }
                    }
                }
            }
            return parametros.Mensagem;
        }

        public string TestaConexao(string String_de_Conexao)
        {
            var Mensagem = "";
            try
            {
                SqlConnection conexao = new SqlConnection(String_de_Conexao);
                conexao.Open();
                Mensagem = "Conexão estabelecida com sucesso !";
                conexao.Close();
            }
            catch (Exception)
            {
                Mensagem = "Erro ao estabelecer conexão, verifique se a string esta correta !!";
            }
            return Mensagem;
        }

        public string ManipulandoStringConexao(string stringConexao, string NomeBanco)
        {
            var stringRetorno = "";
            var inicio_Corte = stringConexao.IndexOf("Catalog=");
            var String_Cortada = stringConexao.Substring(inicio_Corte);
            var fim_Corte = String_Cortada.IndexOf(";");
            var SeparndoCatalog = String_Cortada.Substring(0, fim_Corte);
            stringRetorno = stringConexao.Replace(SeparndoCatalog, string.Format("Catalog={0}", NomeBanco));

            return stringRetorno;
        }

        #region Metodos de criações 
        public void Criacao_De_Estruturas(AtualizacaoDeEstruturasViewModel parametros, string estrutra, string nomeTabela, List<string> colunas_Existentes)
        {

            var NovaString = ManipulandoStringConexao(parametros.String_Conexao, parametros.Banco_Atualizar);
            using (var dapper = new SqlConnection(NovaString))
            {
                string executar = "";
                string comando = string.Format(@"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = '{0}'", nomeTabela);
                List<dynamic> verifica = dapper.Query(comando, commandType: CommandType.Text).ToList();
                if (verifica.Count > 0)
                {
                    var novaLista = new List<string>();
                    for (var x = 0; x < verifica.Count; x++)
                    {
                        string adicionar = Convert.ToString(verifica[x].COLUMN_NAME);
                        novaLista.Add(adicionar.ToUpper());
                    }
                    string[] stringSeparators = new string[] { ",\r\n" };
                    string[] listaColunas = estrutra.Split(stringSeparators, StringSplitOptions.None);
                    for (var x = 0; x < colunas_Existentes.Count; x++)
                    {
                        if (novaLista.Contains(colunas_Existentes[x]))
                        {
                            executar = string.Format(@"ALTER TABLE [{0}] ALTER COLUMN {1}", nomeTabela, listaColunas[x]);
                        }
                        else
                        {
                            executar = string.Format(@"ALTER TABLE [{0}] ADD {1}", nomeTabela, listaColunas[x]);
                        }
                        try
                        {
                            dapper.Execute(executar, commandType: CommandType.Text);
                        }
                        catch (SqlException e)
                        {
                            var erroDeIndice = e.ErrorCode;
                            if (erroDeIndice == -2146232060)
                            {
                                try
                                {
                                    Ex_Excluir_Indices_Temporariamente(dapper, nomeTabela, colunas_Existentes[x], executar);
                                }
                                catch (Exception)
                                {

                                }
                            }
                        }
                    }
                }
                else
                {
                    try
                    {
                        executar = string.Format("CREATE TABLE [{0}] ({1})", nomeTabela, estrutra);
                        dapper.Execute(executar, commandType: CommandType.Text);
                    }
                    catch (SqlException e)
                    {

                    }

                }

            }


        }

        public void Criacao_De_Procedures(AtualizacaoDeEstruturasViewModel parametros, string procedure_estrutura, string nome_procedure)
        {

            var NovaString = ManipulandoStringConexao(parametros.String_Conexao, parametros.Banco_Atualizar);
            using (var dapper = new SqlConnection(NovaString))
            {
                try
                {
                    procedure_estrutura = procedure_estrutura.Replace("procedure", "PROCEDURE");
                    procedure_estrutura = procedure_estrutura.Replace("Procedure", "PROCEDURE");
                    var inicio_corte = procedure_estrutura.LastIndexOf("PROCEDURE");
                    var String_Cortada = procedure_estrutura.Substring(0, inicio_corte);
                    var Executar_Criacao = procedure_estrutura.Replace(String_Cortada, "CREATE or ALTER ");

                    dapper.Execute(Executar_Criacao, commandType: CommandType.Text);
                }
                catch (Exception e)
                {

                }


            }


        }

        public void Criacao_De_Indices(AtualizacaoDeEstruturasViewModel parametros, dynamic dadosIndice, dynamic dadosTabela, dynamic dadosColuna)
        {
            var NovaString = ManipulandoStringConexao(parametros.String_Conexao, parametros.Banco_Atualizar);
            using (var dapper = new SqlConnection(NovaString))
            {
                string comandoVerifcar = string.Format("select * from sys.indexes where name = '{0}'", dadosIndice.name);
                var indiceExiste = dapper.Query(comandoVerifcar, commandType: CommandType.Text).FirstOrDefault();
                if (indiceExiste == null)
                {
                    string buscaTabela = string.Format("select * from sys.tables where name = '{0}'", dadosTabela.name);
                    var tabela = dapper.Query(buscaTabela, commandType: CommandType.Text).FirstOrDefault();
                    if (tabela != null)
                    {
                        string comandoCriarIndice = string.Format("CREATE {0} INDEX [{1}] ON [{2}] ([{3}])", dadosIndice.type_desc, dadosIndice.name, tabela.name, dadosColuna.name);
                        try
                        {
                            dapper.Execute(comandoCriarIndice, commandType: CommandType.Text);
                        }
                        catch (Exception e)
                        {

                        }
                    }
                }
            }
        }

        public void Criacao_De_Chaves_Primarias(AtualizacaoDeEstruturasViewModel parametros, dynamic dadosChavePrimaria)
        {
            var NovaString = ManipulandoStringConexao(parametros.String_Conexao, parametros.Banco_Atualizar);
            using (var dapper = new SqlConnection(NovaString))
            {
                var comandoCriarChave = "";
                string comandoVerificar = string.Format(@"select * from sys.indexes where name = '{0}'", dadosChavePrimaria.CONSTRAINT_NAME);
                var verifica = dapper.Query(comandoVerificar, commandType: CommandType.Text).FirstOrDefault();
                try
                {
                    if (verifica != null)
                    {
                        string comandoDroparChave = string.Format("ALTER TABLE [{0}] DROP CONSTRAINT [{1}]", dadosChavePrimaria.TABLE_NAME, dadosChavePrimaria.CONSTRAINT_NAME);
                        dapper.Execute(comandoDroparChave, commandType: CommandType.Text);
                    }
                    comandoCriarChave = string.Format("CREATE UNIQUE INDEX [{0}] ON [{1}] ({2})", dadosChavePrimaria.CONSTRAINT_NAME, dadosChavePrimaria.TABLE_NAME, dadosChavePrimaria.COLUMN_NAME);
                    dapper.Execute(comandoCriarChave, commandType: CommandType.Text);
                }
                catch (Exception e)
                {

                }
            }
        }

        public void Criacao_De_Foreign(AtualizacaoDeEstruturasViewModel parametros, dynamic ForeignDados, dynamic EstruturaCriacaoForeign, dynamic InfoTabelaReferente)
        {
            var NovaString = ManipulandoStringConexao(parametros.String_Conexao, parametros.Banco_Atualizar);
            using (var dapper = new SqlConnection(NovaString))
            {
                var comandoCriarForeign = "";
                string comandoVerificar = string.Format(@"select * FROM information_schema.key_column_usage where CONSTRAINT_NAME = '{0}'", ForeignDados.CONSTRAINT_NAME);
                var verifica = dapper.Query(comandoVerificar, commandType: CommandType.Text).FirstOrDefault();
                try
                {
                    if (verifica != null)
                    {
                        string comandoDroparForeign = string.Format("ALTER TABLE [{0}] DROP CONSTRAINT [{1}]", ForeignDados.TABLE_NAME, ForeignDados.CONSTRAINT_NAME);
                        dapper.Execute(comandoDroparForeign, commandType: CommandType.Text);
                    }
                    comandoCriarForeign = string.Format("ALTER TABLE [{0}] ADD CONSTRAINT [{1}] FOREIGN KEY ({2}) REFERENCES [{3}] ({4})", ForeignDados.TABLE_NAME, ForeignDados.CONSTRAINT_NAME, ForeignDados.COLUMN_NAME, InfoTabelaReferente.TABLE_NAME, InfoTabelaReferente.COLUMN_NAME);
                    dapper.Execute(comandoCriarForeign, commandType: CommandType.Text);
                }
                catch (Exception e)
                {

                }

            }
        }

        #endregion

        #region Tratamento de exceções
        public void Ex_Excluir_Indices_Temporariamente(SqlConnection dapper, string nomeTabela, string coluna, string refazerComando)
        {
            //Listando dados da tabela (sys.objects) para pegar object_id
            string listarDadosTabela = string.Format(@"select * from sys.objects  where name = '{0}'", nomeTabela);
            var dadosTabela = dapper.Query(listarDadosTabela, commandType: CommandType.Text).FirstOrDefault();

            //Listando colunas para pegar o column_id
            string listaDadosColunas = string.Format(@"select object_id, name, column_id from sys.all_columns where object_id = {0} and name = '{1}'", dadosTabela.object_id, coluna);
            var dadosColuna = dapper.Query(listaDadosColunas, commandType: CommandType.Text).FirstOrDefault();

            //Listando dadosIndiceColumn para buscar Index_id
            string listardadosIndiceColumns = string.Format(@"select * from sys.index_columns where object_id = {0} and column_id = {1}", dadosColuna.object_id, dadosColuna.column_id);
            List<dynamic> dadosIndiceColumns = dapper.Query(listardadosIndiceColumns, commandType: CommandType.Text).ToList();

            var armazenarIndices = new List<dynamic>();
            //Percorrendo lista de indices
            foreach (var item in dadosIndiceColumns)
            {
                string buscarDadosIndice = "";
                //montando comando para buscar indicia de acordo com seu id (sys.indexes) 
                buscarDadosIndice = string.Format("select * from sys.indexes where index_id = {0} and object_id = {1}", item.index_id, item.object_id);
                var dadosIndice = dapper.Query(buscarDadosIndice, commandType: CommandType.Text).FirstOrDefault();

                //Montando comando para excluir o indice
                string deletaIndice = string.Format("Drop Index [{0}] on [{1}]", dadosIndice.name, nomeTabela);
                armazenarIndices.Add(dadosIndice);

                dapper.Execute(deletaIndice, commandType: CommandType.Text);
            }
            //Refazendo comando que estava com erro
            dapper.Execute(refazerComando, commandType: CommandType.Text);

            //Criando novamente os indices
            foreach (var indice in armazenarIndices)
            {
                string recriandoIndices = string.Format("CREATE {0} INDEX [{1}] ON [{2}] ([{3}])", indice.type_desc, indice.name, nomeTabela, coluna);
                dapper.Execute(recriandoIndices, commandType: CommandType.Text);
            }

        }
        #endregion
    }
}
//Data Source=sqlteste.seeger.com.br,10001; Initial Catalog=Seeger; User ID=sweb; Password=Pa$$w0rd20