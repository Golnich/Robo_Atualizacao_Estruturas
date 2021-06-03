using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtualizacaoDeEstruturas.Service
{
    public interface IAtualizacaoEstruturasAppService
    {
        string TestaConexao(string String_de_Conexao);

        string AtualizaBanco(AtualizacaoDeEstruturasViewModel parametros);
    }
}
