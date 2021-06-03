using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AtualizacaoDeEstruturas.Service
{
    public class AtualizacaoDeEstruturasViewModel
    {
        public String String_Conexao { get; set; }
        public string Banco_Principal { get; set; }
        public string Banco_Atualizar { get; set; }
        public bool Estrutura { get; set; }
        public bool Indices { get; set; }
        public bool Procedures { get; set; }
        public bool Chaves_Primarias { get; set; }
        public string Mensagem { get; set; }
    }
}