using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Robo_Atualizacao_De_Estruturas.ViewModels
{
    public class AtualizacaoEstruturas
    {
        public String String_Conexao { get; set; }
        public string Banco_Principal { get; set; }
        public string Banco_Atualizar { get; set; }
        public string Mensagem { get; set; }
        public bool Estrutura { get; set; }
        public bool Indices { get; set; }
        public bool Procedures { get; set; }
        public bool Chaves_Primarias { get; set; }

    }
}