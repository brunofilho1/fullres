import type { SearchFailure } from './types'

/**
 * Human-readable, Brazilian-Portuguese messages for the stable FullRes error codes.
 * Technical provider wording, stack traces and raw payloads are never shown to the user;
 * unknown codes fall back to a generic message that carries the traceId for support.
 */
const GENERIC_MESSAGE =
  'Algo deu errado ao pesquisar esta imagem. Tente novamente. Se o problema continuar, informe o código de referência.'

const MESSAGES: Record<string, string> = {
  // Invalid upload / request
  file_missing: 'Nenhuma imagem foi enviada. Selecione uma imagem e tente novamente.',
  file_empty: 'O arquivo enviado está vazio. Escolha outra imagem.',
  file_invalid: 'A imagem enviada é inválida ou está corrompida. Tente outro arquivo.',
  format_unsupported:
    'O formato desta imagem não é suportado. Envie um arquivo JPG, PNG ou WEBP.',
  file_too_large: 'O arquivo é maior que o limite permitido (20 MB). Tente uma versão menor.',
  image_dimensions_too_large:
    'Esta imagem tem dimensões muito grandes para serem processadas. Tente uma versão menor.',
  image_cannot_be_prepared:
    'Não foi possível preparar esta imagem para a busca. Tente uma versão menor ou com menos detalhes.',
  invalid_request: 'Não foi possível processar a solicitação de busca. Tente novamente.',

  // FullRes own usage limit
  rate_limited:
    'Você fez muitas buscas em pouco tempo. Aguarde alguns instantes e tente novamente.',

  // Provider state
  provider_rate_limited:
    'O serviço de pesquisa está recebendo muitas solicitações agora. Tente novamente em alguns instantes.',
  provider_quota_exceeded:
    'As buscas estão temporariamente indisponíveis porque atingimos o limite de utilização do serviço de pesquisa. Tente novamente mais tarde.',
  provider_not_configured:
    'Não foi possível concluir a busca porque o serviço de pesquisa está temporariamente indisponível. Tente novamente daqui a pouco.',
  provider_credentials_invalid:
    'Não foi possível concluir a busca porque o serviço de pesquisa está temporariamente indisponível. Tente novamente daqui a pouco.',
  provider_unavailable:
    'Não foi possível concluir a busca porque o serviço de pesquisa está temporariamente indisponível. Tente novamente daqui a pouco.',
  search_busy:
    'Há muitas buscas acontecendo agora. Aguarde alguns instantes e tente novamente.',
  provider_rejected_image:
    'O serviço de pesquisa recusou esta imagem. Tente outra imagem ou uma versão com melhor qualidade.',

  // Timeout
  provider_timeout:
    'A busca demorou mais que o esperado e não pôde ser concluída. Verifique sua conexão e tente novamente.',
  client_timeout:
    'A busca demorou mais que o esperado e não pôde ser concluída. Verifique sua conexão e tente novamente.',

  // Network / backend down
  network_error:
    'Não foi possível conectar ao serviço de busca. Verifique sua conexão e tente novamente.',

  // Unexpected
  provider_invalid_response: GENERIC_MESSAGE,
  provider_error: GENERIC_MESSAGE,
  unexpected_error: GENERIC_MESSAGE,
}

/**
 * Converts anything thrown while searching into a friendly failure for the UI.
 * `error` is normally an ApiError, but this must never throw for unexpected values.
 */
export function describeFailure(error: unknown): SearchFailure {
  if (isFailureLike(error)) {
    const known = error.code ? MESSAGES[error.code] : undefined
    return {
      code: error.code ?? 'unknown_error',
      message: known ?? GENERIC_MESSAGE,
      reference: error.reference ?? undefined,
    }
  }

  return { code: 'unknown_error', message: GENERIC_MESSAGE }
}

interface FailureLike {
  code?: string
  reference?: string
}

function isFailureLike(value: unknown): value is FailureLike {
  return typeof value === 'object' && value !== null && typeof (value as { code?: unknown }).code === 'string'
}
