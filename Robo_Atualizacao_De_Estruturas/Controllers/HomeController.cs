using AtualizacaoDeEstruturas.Service;
using Robo_Atualizacao_De_Estruturas.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Robo_Atualizacao_De_Estruturas.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAtualizacaoEstruturasAppService _AtualizaServices;
        public HomeController(IAtualizacaoEstruturasAppService AtualizaServices)
        {
            _AtualizaServices = AtualizaServices;
        }
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contato()
        {
           
            return View();
        }

        public JsonResult TestaConexao(string String_Conexao)
        {      
            var MensagemRetorno = _AtualizaServices.TestaConexao(String_Conexao);
          
            return Json(MensagemRetorno, JsonRequestBehavior.AllowGet);
        }

        public JsonResult AtualizaBancos(AtualizacaoEstruturas parametros)
        {
            var Objeto = new AtualizacaoDeEstruturasViewModel();
            Objeto.String_Conexao = parametros.String_Conexao;
            Objeto.Banco_Principal = parametros.Banco_Principal;
            Objeto.Banco_Atualizar = parametros.Banco_Atualizar;
            Objeto.Estrutura = parametros.Estrutura;
            Objeto.Procedures = parametros.Procedures;
            Objeto.Chaves_Primarias = parametros.Chaves_Primarias;
            Objeto.Indices = parametros.Indices;

            var teste = _AtualizaServices.AtualizaBanco(Objeto);
            var ds_Mensagem = "Foram Atualizados: \n";
            if (parametros.Estrutura == true)
            {
                ds_Mensagem = ds_Mensagem + "- Estruturas \n";
            }
            if (parametros.Indices == true)
            {
                ds_Mensagem = ds_Mensagem + "- Indices \n";
            }
            if (parametros.Procedures == true)
            {
                ds_Mensagem = ds_Mensagem + "- Procedures \n";
            }       
            if (parametros.Chaves_Primarias == true)
            {
                ds_Mensagem = ds_Mensagem + "- Chaves (Primarias e Estrangeiras) \n";
            }
            parametros.Mensagem = ds_Mensagem;
            return Json(parametros, JsonRequestBehavior.AllowGet);
        }
    }
}