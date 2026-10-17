import type { MessageKey } from "./en";

export const ptBR: Record<MessageKey, string> = {
  // Meta
  "meta.title": "FullRes — Encontre a versão em maior resolução",
  "meta.description":
    "FullRes — Encontre o original. Obtenha a melhor resolução. Busca de imagens de código aberto para versões em maior resolução de qualquer imagem.",

  // Header
  "header.languageLabel": "Idioma",
  "header.github": "GitHub",

  // Footer
  "footer.tagline":
    "FullRes — encontre o original, obtenha a melhor resolução. Busca de imagens de código aberto.",
  "footer.poweredBy": "Desenvolvido com",
  "footer.howItWorks": "Como funciona",
  "footer.searchProviders": "Provedores de busca",
  "footer.sourceCode": "Código-fonte",
  "footer.madeBy": "Feito por",

  // Common
  "common.unknown": "Desconhecido",
  "common.untitled": "Resultado sem título",
  "common.reference": "Código de referência: {reference}",

  // Upload screen
  "upload.eyebrow": "Sua imagem, no melhor que pode ser",
  "upload.title": "Encontre a versão em maior resolução.",
  "upload.subtitle":
    "Envie uma imagem. Encontre versões maiores, fontes originais e melhor qualidade pela web.",
  "upload.previewAlt": "Pré-visualização da imagem selecionada",
  "upload.searchButton": "Buscar resoluções maiores",
  "upload.changeImage": "Trocar imagem",
  "upload.chooseImage": "Escolher uma imagem",
  "upload.dropzoneAria": "Solte uma imagem aqui ou procure arquivos",
  "upload.dropTitle": "Solte sua imagem aqui",
  "upload.dropSub": "ou procure arquivos no seu dispositivo",
  "upload.dropHint": "JPG · PNG · WEBP · Até 20 MB",
  "upload.or": "ou",
  "upload.urlPlaceholder": "Ou cole a URL de uma imagem...",
  "upload.useUrl": "Usar URL",
  "upload.imageUrl": "URL da imagem",
  "upload.pasteNote":
    "Você também pode colar uma imagem da área de transferência",
  "upload.trustNote": "Sem necessidade de conta · Simples e transparente",
  "upload.errorFormat":
    "Formato não suportado. Use um arquivo JPG, PNG ou WEBP.",
  "upload.errorTooLarge":
    "O arquivo é maior que 20 MB. Tente uma versão menor.",
  "upload.errorImageTooLarge":
    "A imagem é maior que 20 MB. Tente uma versão menor.",
  "upload.errorCorrupt":
    "Não foi possível ler essa imagem. Ela pode estar corrompida.",
  "upload.errorInvalidUrl": "Isso não parece ser uma URL de imagem válida.",
  "upload.errorProtocol": "Apenas URLs de imagem http e https são suportadas.",
  "upload.errorHostStatus":
    "O host retornou HTTP {status} para essa URL de imagem.",
  "upload.errorNotImage":
    "Essa URL não aponta para uma imagem JPG, PNG ou WEBP.",
  "upload.errorDownload":
    "O navegador não conseguiu baixar essa imagem. O host pode bloquear o acesso direto — baixe o arquivo e envie-o em vez disso.",
  "upload.howEyebrow": "Como funciona",
  "upload.howStep1Title": "Envie",
  "upload.howStep1Desc": "Escolha qualquer imagem.",
  "upload.howStep2Title": "Busque",
  "upload.howStep2Desc": "Encontre versões correspondentes.",
  "upload.howStep3Title": "Recupere",
  "upload.howStep3Desc": "Obtenha a melhor versão.",
  "upload.howDisclaimer":
    "As buscas passam por índices públicos de busca reversa de imagens. Apenas a imagem que você enviar é enviada ao provedor de busca.",

  // Search progress
  "progress.title": "Buscando resoluções maiores",
  "progress.step1Label": "Lendo sua imagem",
  "progress.step1Desc":
    "Dimensões, formato e tamanho do arquivo foram lidos no seu navegador.",
  "progress.step2Label": "Buscando em índices de imagens",
  "progress.step2Desc":
    "Enviando sua imagem e classificando os resultados retornados pelo provedor.",
  "progress.tip1": "Talvez o servidor tenha caído…",
  "progress.tip2": "Talvez o SerpApi tenha caído…",
  "progress.tip3":
    "Tá, talvez seja só o nosso péssimo código que não funciona…",
  "progress.tip4": "Talvez seja só uma imagem bem grande.",
  "progress.tip5":
    "O hamster que liga o servidor provavelmente está no intervalo.",
  "progress.tip6": "A internet é um lugar enorme. Dá um instante.",
  "progress.tip7":
    "Ainda é mais rápido que procurar a imagem na mão. Provavelmente.",
  "progress.tip8": "Essa é a parte divertida. Qualquer segundo agora.",
  "progress.tip9": "Talvez alguém tenha desligado o cabo errado.",
  "progress.tip10": "Procurando em cada canto da web. Até nos mais estranhos.",
  "progress.tip11": "Paciência é uma virtude. Dizem.",
  "progress.tip12": "Você já tentou desligar e ligar de novo? Agora é tarde.",
  "progress.tip13": "Os resultados vão valer a pena. Esperamos.",
  "progress.tip14": "Bem, talvez seja melhor se você tentar localmente.",

  // Results screen
  "results.newSearch": "Nova busca",
  "results.title": "Resultados da busca",
  "results.subtitleEmpty": "Nenhum resultado encontrado",
  "results.subtitleError": "Busca interrompida",
  "results.possibleMatches_one": "{count} possível resultado · {sort}",
  "results.possibleMatches_other": "{count} possíveis resultados · {sort}",
  "results.sortAria": "Ordenar resultados",
  "results.sortResolution": "Maior resolução",
  "results.sortSimilarity": "Maior similaridade",
  "results.sortSize": "Arquivo mais leve",
  "results.sortedByResolution": "Ordenado por resolução",
  "results.sortedBySimilarity": "Ordenado por similaridade",
  "results.sortedBySize": "Ordenado pelo arquivo mais leve",
  "results.errorTitle": "Algo deu errado",
  "results.tryAgain": "Tentar novamente",
  "results.useAnotherImage": "Usar outra imagem",
  "results.yourOriginal": "Seu original",
  "results.originalAlt": "Sua imagem enviada",
  "results.specDimensions": "Dimensões",
  "results.specPixelCount": "Total de pixels",
  "results.specQuality": "Qualidade",
  "results.specFormat": "Formato",
  "results.specRatio": "Proporção",
  "results.specFileSize": "Tamanho do arquivo",
  "results.latestSearchFailed": "A última busca falhou",
  "results.partialResults": "Resultados parciais",
  "results.filters": "Filtros",
  "results.minResolutionAria": "Resolução mínima",
  "results.formatAria": "Formato",
  "results.ratioAria": "Proporção",
  "results.visualMatch": "Similaridade visual",
  "results.minVisualMatchAria": "Similaridade visual mínima",
  "results.countOf": "{visible} de {total}",
  "results.emptyTitle": "Nenhum resultado encontrado",
  "results.emptyBody":
    "Não encontramos outra cópia dessa imagem. Ela pode ser muito recente, recortada ou nunca ter sido publicada. Tente uma versão menos recortada.",
  "results.tryAnotherImage": "Tentar outra imagem",
  "results.noFilterMatchTitle": "Nenhum resultado atende aos seus filtros",
  "results.foundMatches_one":
    "{count} resultado foi encontrado, mas nenhum atende à combinação atual de filtros.",
  "results.foundMatches_other":
    "{count} resultados foram encontrados, mas nenhum atende à combinação atual de filtros.",
  "results.clearFilters": "Limpar filtros",
  "results.largestDisclaimer":
    "A maior imagem nem sempre é o original ou a melhor qualidade — confira a fonte antes de baixar.",
  "results.resultsProvidedBy": "Resultados fornecidos por {provider}.",
  "results.verifiedNote":
    "As dimensões marcadas como verificadas foram lidas diretamente da imagem de origem; as demais vêm do índice de busca e podem ser diferentes.",
  "filters.anyResolution": "Qualquer resolução",
  "filters.anyFormat": "Qualquer formato",
  "filters.anyRatio": "Qualquer proporção",

  // Result card
  "card.bestResolution": "MELHOR RESOLUÇÃO",
  "card.dimensionsUnknown": "Dimensões desconhecidas",
  "card.unknownSource": "fonte desconhecida",
  "card.visualMatch": "Similaridade visual",
  "card.verified": "verificada",
  "card.reported": "informada",
  "card.verifiedTitle": "Lida diretamente da imagem de origem",
  "card.reportedTitle": "Informada pelo índice de busca, não verificada",
  "card.viewSource": "Ver página de origem",
  "card.downloadImage": "Baixar imagem",
  "card.compareWithMine": "Comparar com a minha",
  "card.compareAria": "Comparar {title}",

  // Compare modal
  "compare.title": "Comparando versões",
  "compare.unknownSource": "fonte desconhecida",
  "compare.dialogAria": "Comparar a imagem original e a encontrada",
  "compare.zoomIn": "Aumentar zoom",
  "compare.zoomOut": "Diminuir zoom",
  "compare.close": "Fechar comparação",
  "compare.original": "Original",
  "compare.found": "Encontrada",
  "compare.yourOriginal": "Seu original",
  "compare.foundVersion": "Versão encontrada",
  "compare.unknownDimensions": "Dimensões desconhecidas",
  "compare.resolutionGain": "Ganho de resolução",
  "compare.resolutionChange": "Variação de resolução",
  "compare.noDimensions":
    "Dimensões da fonte indisponíveis para este resultado",
  "compare.visualMatchSuffix": "· {similarity}% de similaridade visual",
  "compare.download": "Baixar",
  "compare.openSource": "Abrir página de origem",

  // Formatting helpers
  "format.morePixels": "{ratio}× mais pixels",
  "format.fewerPixels": "{ratio}× menos pixels",
  "format.samePixels": "Mesma quantidade de pixels",

  // Thumb fallback
  "thumb.previewUnavailable": "{alt} (pré-visualização indisponível)",

  // How it works page
  "how.back": "Voltar para a busca",
  "how.eyebrow": "Como funciona",
  "how.title": "De uma única imagem à melhor versão na web",
  "how.lead":
    "O FullRes analisa a imagem que você já tem, busca em índices públicos de busca reversa de imagens e então verifica e classifica cada candidato antes de mostrá-lo. Aqui está exatamente o que acontece nos bastidores.",
  "how.step1Title": "Envie sua imagem",
  "how.step1Desc":
    "Arraste e solte um arquivo, procure no seu dispositivo, cole da área de transferência (Ctrl+V) ou cole a URL de uma imagem remota. JPG, PNG e WEBP de até 20 MB são suportados.",
  "how.step2Title": "Leia localmente",
  "how.step2Desc":
    "Seu navegador lê as dimensões, o formato e o tamanho do arquivo antes de qualquer coisa ser enviada. Nada sai do seu dispositivo até você iniciar a busca.",
  "how.step3Title": "Prepare a requisição",
  "how.step3Desc":
    "No servidor a imagem é recodificada com SkiaSharp para se adequar aos limites do provedor, sem alterar o que a imagem mostra.",
  "how.step4Title": "Busque nos índices",
  "how.step4Desc":
    "A imagem preparada é enviada a um provedor de busca reversa de imagens — atualmente o Google Lens por meio do SerpApi. Apenas a imagem que você enviou é transmitida, e nenhuma conta ou dado pessoal é necessário.",
  "how.step5Title": "Verifique cada resultado",
  "how.step5Desc":
    "Cada candidato é conferido contra o arquivo real. Quando as dimensões podem ser lidas diretamente da imagem de origem ele é marcado como verificado; caso contrário, permanece informado, exatamente como o índice retornou.",
  "how.step6Title": "Remova duplicatas, filtre e classifique",
  "how.step6Desc":
    "URLs duplicadas são mescladas, resultados inválidos ou grandes demais são descartados, e o restante é classificado por resolução, similaridade visual e tamanho do arquivo — com o melhor resultado destacado.",
  "how.noteStrong": "Tudo o que aparece na tela é real.",
  "how.noteBody":
    "Dimensões, formatos, tamanhos e pontuações de similaridade que não puderam ser medidos aparecem como —, nunca inventados. A maior imagem é apenas o maior resultado que foi encontrado — não necessariamente o original.",

  // Providers page
  "providers.back": "Voltar para a busca",
  "providers.eyebrow": "Provedores de busca",
  "providers.title": "Os mecanismos por trás da sua busca",
  "providers.lead":
    "O FullRes não rastreia a web por conta própria. Ele consulta provedores de busca reversa de imagens e então verifica e classifica o que eles retornam. Hoje há um provedor conectado, e o pipeline foi construído para que mais possam ser adicionados.",
  "providers.activeTitle": "Provedores ativos",
  "providers.activeNote":
    "As buscas passam por índices públicos de busca reversa de imagens. Apenas a imagem que você enviar é enviada ao provedor.",
  "providers.active": "Ativo",
  "providers.serpapiDesc":
    "Busca reversa de imagens com tecnologia do Google Lens, acessada por meio do serviço SerpApi. Cada busca no FullRes consome uma consulta da cota mensal do plano da API.",
  "providers.pipelineTitle": "Como o pipeline funciona",
  "providers.pipelineLead":
    "Um provedor tem uma única tarefa: retornar candidatos. Tudo depois disso é tratado pelo FullRes, então um novo provedor nunca precisa reimplementar as partes difíceis.",
  "providers.pipeline1Title": "O provedor retorna candidatos",
  "providers.pipeline1Desc":
    "Ele recebe a imagem preparada e devolve uma lista de possíveis resultados, cada um com o que o mecanismo sabe: URL da imagem, página de origem, resolução, formato e similaridade.",
  "providers.pipeline2Title": "O FullRes os verifica",
  "providers.pipeline2Desc":
    "Cada candidato é conferido contra o arquivo real. As dimensões que podem ser lidas diretamente da fonte são marcadas como verificadas; as demais permanecem informadas, exatamente como o índice as retornou.",
  "providers.pipeline3Title":
    "O FullRes remove duplicatas, filtra e classifica",
  "providers.pipeline3Desc":
    "URLs duplicadas são mescladas, resultados inválidos ou grandes demais são descartados, e o restante é ordenado por resolução, similaridade e tamanho do arquivo. Essa lógica é compartilhada por todos os provedores.",
  "providers.addTitle": "Adicione seu próprio provedor",
  "providers.addIntro": "Novos provedores de busca são bem-vindos. Implemente",
  "providers.addMiddle": "em",
  "providers.addOutro":
    "e retorne uma lista de imagens candidatas — a verificação, a remoção de duplicatas e a classificação são tratadas pelo pipeline.",
  "providers.noteStrong": "Os provedores nunca mudam o que é exibido.",
  "providers.noteBody":
    "Dimensões, formatos e tamanhos desconhecidos permanecem desconhecidos, e os resultados nunca são inventados para preencher lacunas. Se um provedor não puder responder, isso é informado com honestidade em vez de ser escondido.",
  "providers.viewInterface": "Ver a interface de provedor no GitHub",

  // Warnings (provider pipeline)
  "warnings.duplicates_removed_one": "{count} resultado duplicado removido.",
  "warnings.duplicates_removed_other":
    "{count} resultados duplicados removidos.",
  "warnings.hidden_by_resolution_filter_one":
    "{count} resultado ocultado pelo filtro de resolução (dimensões desconhecidas ou pequenas demais).",
  "warnings.hidden_by_resolution_filter_other":
    "{count} resultados ocultados pelo filtro de resolução (dimensões desconhecidas ou pequenas demais).",
  "warnings.hidden_by_format_filter_one":
    "{count} resultado ocultado pelo filtro de formato (não foi possível verificar o formato).",
  "warnings.hidden_by_format_filter_other":
    "{count} resultados ocultados pelo filtro de formato (não foi possível verificar o formato).",
  "warnings.similarity_filter_ignored":
    "Filtro de similaridade ignorado: o provedor de busca não fornece pontuações de similaridade.",
  "warnings.similarity_sort_fallback":
    "Ordenado por resolução em vez disso: o provedor de busca não fornece pontuações de similaridade.",
  "warnings.unverified_matches_one":
    "{count} resultado não pôde ser verificado contra a imagem de origem (host inacessível ou bloqueado); as dimensões exibidas vêm do índice de busca.",
  "warnings.unverified_matches_other":
    "{count} resultados não puderam ser verificados contra a imagem de origem (host inacessível ou bloqueado); as dimensões exibidas vêm do índice de busca.",

  // Errors (stable API codes)
  "errors.generic":
    "Algo deu errado ao buscar esta imagem. Tente novamente. Se o problema persistir, informe o código de referência.",
  "errors.file_missing":
    "Nenhuma imagem foi enviada. Selecione uma imagem e tente novamente.",
  "errors.file_empty": "O arquivo enviado está vazio. Escolha outra imagem.",
  "errors.file_invalid":
    "A imagem enviada é inválida ou está corrompida. Tente outro arquivo.",
  "errors.format_unsupported":
    "Este formato de imagem não é suportado. Envie um arquivo JPG, PNG ou WEBP.",
  "errors.file_too_large":
    "O arquivo é maior que o limite permitido (20 MB). Tente uma versão menor.",
  "errors.image_dimensions_too_large":
    "Esta imagem tem dimensões grandes demais para serem processadas. Tente uma versão menor.",
  "errors.image_cannot_be_prepared":
    "Não foi possível preparar esta imagem para a busca. Tente uma versão menor ou com menos detalhes.",
  "errors.invalid_request":
    "Não foi possível processar a requisição de busca. Tente novamente.",
  "errors.rate_limited":
    "Você fez muitas buscas em pouco tempo. Aguarde um momento e tente novamente.",
  "errors.provider_rate_limited":
    "O serviço de busca está recebendo muitas requisições no momento. Tente novamente em instantes.",
  "errors.provider_quota_exceeded":
    "A busca está temporariamente indisponível porque atingimos o limite de uso do serviço de busca. Tente novamente mais tarde.",
  "errors.provider_not_configured":
    "A busca não pôde ser concluída porque o serviço de busca está temporariamente indisponível. Tente novamente em breve.",
  "errors.provider_credentials_invalid":
    "A busca não pôde ser concluída porque o serviço de busca está temporariamente indisponível. Tente novamente em breve.",
  "errors.provider_unavailable":
    "A busca não pôde ser concluída porque o serviço de busca está temporariamente indisponível. Tente novamente em breve.",
  "errors.search_busy":
    "Há muitas buscas acontecendo agora. Aguarde um momento e tente novamente.",
  "errors.provider_rejected_image":
    "O serviço de busca rejeitou esta imagem. Tente outra imagem ou uma versão de maior qualidade.",
  "errors.provider_timeout":
    "A busca demorou mais que o esperado e não pôde ser concluída. Verifique sua conexão e tente novamente.",
  "errors.client_timeout":
    "A busca demorou mais que o esperado e não pôde ser concluída. Verifique sua conexão e tente novamente.",
  "errors.network_error":
    "Não foi possível conectar ao serviço de busca. Verifique sua conexão e tente novamente.",
  "errors.provider_invalid_response":
    "Algo deu errado ao buscar esta imagem. Tente novamente. Se o problema persistir, informe o código de referência.",
  "errors.provider_error":
    "Algo deu errado ao buscar esta imagem. Tente novamente. Se o problema persistir, informe o código de referência.",
  "errors.unexpected_error":
    "Algo deu errado ao buscar esta imagem. Tente novamente. Se o problema persistir, informe o código de referência.",
  "errors.unknown_error":
    "Algo deu errado ao buscar esta imagem. Tente novamente. Se o problema persistir, informe o código de referência.",
};
