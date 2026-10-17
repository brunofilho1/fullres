import type { MessageKey } from "./en";

export const es: Record<MessageKey, string> = {
  // Meta
  "meta.title": "FullRes — Encuentra la versión de mayor resolución",
  "meta.description":
    "FullRes — Encuentra el original. Obtén la mejor resolución. Búsqueda de imágenes de código abierto para versiones de mayor resolución de cualquier imagen.",

  // Header
  "header.languageLabel": "Idioma",
  "header.github": "GitHub",

  // Footer
  "footer.tagline":
    "FullRes — encuentra el original, obtén la mejor resolución. Búsqueda de imágenes de código abierto.",
  "footer.poweredBy": "Con la tecnología de",
  "footer.howItWorks": "Cómo funciona",
  "footer.searchProviders": "Proveedores de búsqueda",
  "footer.sourceCode": "Código fuente",
  "footer.madeBy": "Hecho por",

  // Common
  "common.unknown": "Desconocido",
  "common.untitled": "Coincidencia sin título",
  "common.reference": "Código de referencia: {reference}",

  // Upload screen
  "upload.eyebrow": "Tu imagen, en su mejor versión",
  "upload.title": "Encuentra la versión de mayor resolución.",
  "upload.subtitle":
    "Sube una imagen. Encuentra versiones más grandes, fuentes originales y mejor calidad por toda la web.",
  "upload.previewAlt": "Vista previa de la imagen seleccionada",
  "upload.searchButton": "Buscar resoluciones mayores",
  "upload.changeImage": "Cambiar imagen",
  "upload.chooseImage": "Elegir una imagen",
  "upload.dropzoneAria": "Suelta una imagen aquí o busca archivos",
  "upload.dropTitle": "Suelta tu imagen aquí",
  "upload.dropSub": "o busca archivos en tu dispositivo",
  "upload.dropHint": "JPG · PNG · WEBP · Hasta 20 MB",
  "upload.or": "o",
  "upload.urlPlaceholder": "O pega la URL de una imagen...",
  "upload.useUrl": "Usar URL",
  "upload.imageUrl": "URL de la imagen",
  "upload.pasteNote": "También puedes pegar una imagen del portapapeles",
  "upload.trustNote": "Sin necesidad de cuenta · Simple y transparente",
  "upload.errorFormat":
    "Formato no compatible. Usa un archivo JPG, PNG o WEBP.",
  "upload.errorTooLarge":
    "Ese archivo supera los 20 MB. Prueba con una versión más pequeña.",
  "upload.errorImageTooLarge":
    "Esa imagen supera los 20 MB. Prueba con una versión más pequeña.",
  "upload.errorCorrupt": "No se pudo leer esa imagen. Puede estar dañada.",
  "upload.errorInvalidUrl": "Eso no parece ser una URL de imagen válida.",
  "upload.errorProtocol": "Solo se admiten URLs de imagen http y https.",
  "upload.errorHostStatus":
    "El host devolvió HTTP {status} para esa URL de imagen.",
  "upload.errorNotImage": "Esa URL no apunta a una imagen JPG, PNG o WEBP.",
  "upload.errorDownload":
    "El navegador no pudo descargar esa imagen. El host puede estar bloqueando el acceso directo — descarga el archivo y súbelo en su lugar.",
  "upload.howEyebrow": "Cómo funciona",
  "upload.howStep1Title": "Sube",
  "upload.howStep1Desc": "Elige cualquier imagen.",
  "upload.howStep2Title": "Busca",
  "upload.howStep2Desc": "Encuentra versiones coincidentes.",
  "upload.howStep3Title": "Recupera",
  "upload.howStep3Desc": "Obtén la mejor versión.",
  "upload.howDisclaimer":
    "Las búsquedas usan índices públicos de búsqueda inversa de imágenes. Solo la imagen que envías se transmite al proveedor de búsqueda.",

  // Search progress
  "progress.title": "Buscando resoluciones mayores",
  "progress.step1Label": "Leyendo tu imagen",
  "progress.step1Desc":
    "Las dimensiones, el formato y el tamaño del archivo se leyeron en tu navegador.",
  "progress.step2Label": "Buscando en índices de imágenes",
  "progress.step2Desc":
    "Subiendo tu imagen y clasificando las coincidencias que devuelve el proveedor.",
  "progress.tip1": "Quizá el servidor se cayó…",
  "progress.tip2": "Quizá SerpApi se cayó…",
  "progress.tip3":
    "Vale, quizá solo sea nuestro pésimo código que no funciona…",
  "progress.tip4": "Quizá solo sea una imagen muy grande.",
  "progress.tip5":
    "El hámster que enciende el servidor probablemente está en su descanso.",
  "progress.tip6": "Internet es un lugar enorme. Dale un momento.",
  "progress.tip7":
    "Sigue siendo más rápido que buscar la imagen a mano. Probablemente.",
  "progress.tip8": "Esta es la parte divertida. En cualquier momento.",
  "progress.tip9": "Quizá alguien desconectó el cable equivocado.",
  "progress.tip10":
    "Buscando en cada rincón de la web. Hasta en los más raros.",
  "progress.tip11": "La paciencia es una virtud. Eso dicen.",
  "progress.tip12":
    "¿Ya intentaste apagarlo y encenderlo otra vez? Ya es tarde para eso.",
  "progress.tip13": "Los resultados valdrán la pena. Eso esperamos.",
  "progress.tip14": "Bien, tal vez sea mejor si intentas localmente.",

  // Results screen
  "results.newSearch": "Nueva búsqueda",
  "results.title": "Resultados de la búsqueda",
  "results.subtitleEmpty": "No se encontraron coincidencias",
  "results.subtitleError": "Búsqueda interrumpida",
  "results.possibleMatches_one": "{count} posible coincidencia · {sort}",
  "results.possibleMatches_other": "{count} posibles coincidencias · {sort}",
  "results.sortAria": "Ordenar resultados",
  "results.sortResolution": "Mayor resolución",
  "results.sortSimilarity": "Mayor similitud",
  "results.sortSize": "Archivo más ligero",
  "results.sortedByResolution": "Ordenado por resolución",
  "results.sortedBySimilarity": "Ordenado por similitud",
  "results.sortedBySize": "Ordenado por el archivo más ligero",
  "results.errorTitle": "Algo salió mal",
  "results.tryAgain": "Intentar de nuevo",
  "results.useAnotherImage": "Usar otra imagen",
  "results.yourOriginal": "Tu original",
  "results.originalAlt": "Tu imagen subida",
  "results.specDimensions": "Dimensiones",
  "results.specPixelCount": "Total de píxeles",
  "results.specQuality": "Calidad",
  "results.specFormat": "Formato",
  "results.specRatio": "Proporción",
  "results.specFileSize": "Tamaño del archivo",
  "results.latestSearchFailed": "La última búsqueda falló",
  "results.partialResults": "Resultados parciales",
  "results.filters": "Filtros",
  "results.minResolutionAria": "Resolución mínima",
  "results.formatAria": "Formato",
  "results.ratioAria": "Proporción",
  "results.visualMatch": "Similitud visual",
  "results.minVisualMatchAria": "Similitud visual mínima",
  "results.countOf": "{visible} de {total}",
  "results.emptyTitle": "No se encontraron coincidencias",
  "results.emptyBody":
    "No encontramos otra copia de esta imagen. Puede ser muy reciente, estar recortada o no haberse publicado nunca. Prueba con una versión menos recortada.",
  "results.tryAnotherImage": "Probar otra imagen",
  "results.noFilterMatchTitle": "Ninguna coincidencia cumple tus filtros",
  "results.foundMatches_one":
    "Se encontró {count} coincidencia, pero ninguna cumple la combinación de filtros actual.",
  "results.foundMatches_other":
    "Se encontraron {count} coincidencias, pero ninguna cumple la combinación de filtros actual.",
  "results.clearFilters": "Limpiar filtros",
  "results.largestDisclaimer":
    "La imagen más grande no siempre es el original ni la mejor calidad — revisa la fuente antes de descargarla.",
  "results.resultsProvidedBy": "Resultados proporcionados por {provider}.",
  "results.verifiedNote":
    "Las dimensiones marcadas como verificadas se leyeron directamente de la imagen de origen; las demás provienen del índice de búsqueda y pueden ser distintas.",
  "filters.anyResolution": "Cualquier resolución",
  "filters.anyFormat": "Cualquier formato",
  "filters.anyRatio": "Cualquier proporción",

  // Result card
  "card.bestResolution": "MEJOR RESOLUCIÓN",
  "card.dimensionsUnknown": "Dimensiones desconocidas",
  "card.unknownSource": "fuente desconocida",
  "card.visualMatch": "Similitud visual",
  "card.verified": "verificada",
  "card.reported": "informada",
  "card.verifiedTitle": "Leída directamente de la imagen de origen",
  "card.reportedTitle": "Informada por el índice de búsqueda, sin verificar",
  "card.viewSource": "Ver página de origen",
  "card.downloadImage": "Descargar imagen",
  "card.compareWithMine": "Comparar con la mía",
  "card.compareAria": "Comparar {title}",

  // Compare modal
  "compare.title": "Comparando versiones",
  "compare.unknownSource": "fuente desconocida",
  "compare.dialogAria": "Comparar la imagen original y la encontrada",
  "compare.zoomIn": "Acercar",
  "compare.zoomOut": "Alejar",
  "compare.close": "Cerrar comparación",
  "compare.original": "Original",
  "compare.found": "Encontrada",
  "compare.yourOriginal": "Tu original",
  "compare.foundVersion": "Versión encontrada",
  "compare.unknownDimensions": "Dimensiones desconocidas",
  "compare.resolutionGain": "Ganancia de resolución",
  "compare.resolutionChange": "Cambio de resolución",
  "compare.noDimensions":
    "Dimensiones de origen no disponibles para esta coincidencia",
  "compare.visualMatchSuffix": "· {similarity}% de similitud visual",
  "compare.download": "Descargar",
  "compare.openSource": "Abrir página de origen",

  // Formatting helpers
  "format.morePixels": "{ratio}× más píxeles",
  "format.fewerPixels": "{ratio}× menos píxeles",
  "format.samePixels": "Misma cantidad de píxeles",

  // Thumb fallback
  "thumb.previewUnavailable": "{alt} (vista previa no disponible)",

  // How it works page
  "how.back": "Volver a la búsqueda",
  "how.eyebrow": "Cómo funciona",
  "how.title": "De una sola imagen a la mejor versión de la web",
  "how.lead":
    "FullRes analiza la imagen que ya tienes, busca en índices públicos de búsqueda inversa de imágenes y luego verifica y clasifica cada candidata antes de mostrártela. Esto es exactamente lo que ocurre entre bastidores.",
  "how.step1Title": "Sube tu imagen",
  "how.step1Desc":
    "Arrastra y suelta un archivo, busca en tu dispositivo, pega desde el portapapeles (Ctrl+V) o pega la URL de una imagen remota. Se admiten JPG, PNG y WEBP de hasta 20 MB.",
  "how.step2Title": "Léela localmente",
  "how.step2Desc":
    "Tu navegador lee las dimensiones, el formato y el tamaño del archivo antes de enviar nada. Nada sale de tu dispositivo hasta que inicias la búsqueda.",
  "how.step3Title": "Prepara la solicitud",
  "how.step3Desc":
    "En el servidor la imagen se recodifica con SkiaSharp para ajustarse a los límites del proveedor, sin cambiar lo que muestra la imagen.",
  "how.step4Title": "Busca en los índices",
  "how.step4Desc":
    "La imagen preparada se envía a un proveedor de búsqueda inversa de imágenes — actualmente Google Lens a través de SerpApi. Solo se transmite la imagen que enviaste, y no se requiere ninguna cuenta ni dato personal.",
  "how.step5Title": "Verifica cada coincidencia",
  "how.step5Desc":
    "Cada candidata se comprueba contra el archivo real. Cuando las dimensiones pueden leerse directamente de la imagen de origen se marca como verificada; de lo contrario queda como informada, tal como la devolvió el índice.",
  "how.step6Title": "Elimina duplicados, filtra y clasifica",
  "how.step6Desc":
    "Las URL duplicadas se fusionan, los resultados inválidos o demasiado grandes se descartan, y el resto se clasifica por resolución, similitud visual y tamaño de archivo — con la mejor coincidencia destacada.",
  "how.noteStrong": "Todo lo que aparece en pantalla es real.",
  "how.noteBody":
    "Las dimensiones, formatos, tamaños y puntuaciones de similitud que no pudieron medirse se muestran como —, nunca se inventan. La imagen más grande es solo la mayor coincidencia que se encontró — no necesariamente el original.",

  // Providers page
  "providers.back": "Volver a la búsqueda",
  "providers.eyebrow": "Proveedores de búsqueda",
  "providers.title": "Los motores detrás de tu búsqueda",
  "providers.lead":
    "FullRes no rastrea la web por su cuenta. Consulta proveedores de búsqueda inversa de imágenes y luego verifica y clasifica lo que devuelven. Hoy hay un proveedor conectado, y el pipeline está construido para que se puedan añadir más.",
  "providers.activeTitle": "Proveedores activos",
  "providers.activeNote":
    "Las búsquedas usan índices públicos de búsqueda inversa de imágenes. Solo la imagen que envías se transmite al proveedor.",
  "providers.active": "Activo",
  "providers.serpapiDesc":
    "Búsqueda inversa de imágenes con la tecnología de Google Lens, accedida a través del servicio SerpApi. Cada búsqueda en FullRes consume una consulta de la cuota mensual del plan de la API.",
  "providers.pipelineTitle": "Cómo funciona el pipeline",
  "providers.pipelineLead":
    "Un proveedor tiene una única tarea: devolver candidatas. Todo lo demás lo gestiona FullRes, así que un nuevo proveedor nunca tiene que reimplementar las partes difíciles.",
  "providers.pipeline1Title": "El proveedor devuelve candidatas",
  "providers.pipeline1Desc":
    "Recibe la imagen preparada y devuelve una lista de posibles coincidencias, cada una con lo que sabe el motor: URL de la imagen, página de origen, resolución, formato y similitud.",
  "providers.pipeline2Title": "FullRes las verifica",
  "providers.pipeline2Desc":
    "Cada candidata se comprueba contra el archivo real. Las dimensiones que pueden leerse directamente de la fuente se marcan como verificadas; las demás quedan como informadas, tal como las devolvió el índice.",
  "providers.pipeline3Title": "FullRes elimina duplicados, filtra y clasifica",
  "providers.pipeline3Desc":
    "Las URL duplicadas se fusionan, los resultados inválidos o demasiado grandes se descartan, y el resto se ordena por resolución, similitud y tamaño de archivo. Esta lógica es compartida por todos los proveedores.",
  "providers.addTitle": "Añade tu propio proveedor",
  "providers.addIntro":
    "Los nuevos proveedores de búsqueda son bienvenidos. Implementa",
  "providers.addMiddle": "en",
  "providers.addOutro":
    "y devuelve una lista de imágenes candidatas — la verificación, la eliminación de duplicados y la clasificación las gestiona el pipeline.",
  "providers.noteStrong": "Los proveedores nunca cambian lo que se muestra.",
  "providers.noteBody":
    "Las dimensiones, formatos y tamaños desconocidos siguen siendo desconocidos, y los resultados nunca se inventan para rellenar un hueco. Si un proveedor no puede responder, se informa con honestidad en lugar de ocultarlo.",
  "providers.viewInterface": "Ver la interfaz de proveedor en GitHub",

  // Warnings (provider pipeline)
  "warnings.duplicates_removed_one": "Se eliminó {count} resultado duplicado.",
  "warnings.duplicates_removed_other":
    "Se eliminaron {count} resultados duplicados.",
  "warnings.hidden_by_resolution_filter_one":
    "Se ocultó {count} resultado por el filtro de resolución (dimensiones desconocidas o demasiado pequeñas).",
  "warnings.hidden_by_resolution_filter_other":
    "Se ocultaron {count} resultados por el filtro de resolución (dimensiones desconocidas o demasiado pequeñas).",
  "warnings.hidden_by_format_filter_one":
    "Se ocultó {count} resultado por el filtro de formato (no se pudo verificar el formato).",
  "warnings.hidden_by_format_filter_other":
    "Se ocultaron {count} resultados por el filtro de formato (no se pudo verificar el formato).",
  "warnings.similarity_filter_ignored":
    "Filtro de similitud ignorado: el proveedor de búsqueda no proporciona puntuaciones de similitud.",
  "warnings.similarity_sort_fallback":
    "Se ordenó por resolución en su lugar: el proveedor de búsqueda no proporciona puntuaciones de similitud.",
  "warnings.unverified_matches_one":
    "{count} coincidencia no pudo verificarse contra la imagen de origen (host inaccesible o bloqueado); las dimensiones mostradas provienen del índice de búsqueda.",
  "warnings.unverified_matches_other":
    "{count} coincidencias no pudieron verificarse contra la imagen de origen (host inaccesible o bloqueado); las dimensiones mostradas provienen del índice de búsqueda.",

  // Errors (stable API codes)
  "errors.generic":
    "Algo salió mal al buscar esta imagen. Inténtalo de nuevo. Si el problema persiste, informa del código de referencia.",
  "errors.file_missing":
    "No se subió ninguna imagen. Selecciona una imagen e inténtalo de nuevo.",
  "errors.file_empty": "El archivo subido está vacío. Elige otra imagen.",
  "errors.file_invalid":
    "La imagen subida no es válida o está dañada. Prueba con otro archivo.",
  "errors.format_unsupported":
    "Este formato de imagen no es compatible. Sube un archivo JPG, PNG o WEBP.",
  "errors.file_too_large":
    "El archivo supera el límite permitido (20 MB). Prueba con una versión más pequeña.",
  "errors.image_dimensions_too_large":
    "Esta imagen tiene dimensiones demasiado grandes para procesarse. Prueba con una versión más pequeña.",
  "errors.image_cannot_be_prepared":
    "No se pudo preparar esta imagen para la búsqueda. Prueba con una versión más pequeña o con menos detalles.",
  "errors.invalid_request":
    "No se pudo procesar la solicitud de búsqueda. Inténtalo de nuevo.",
  "errors.rate_limited":
    "Has hecho demasiadas búsquedas en poco tiempo. Espera un momento e inténtalo de nuevo.",
  "errors.provider_rate_limited":
    "El servicio de búsqueda está recibiendo demasiadas solicitudes ahora mismo. Inténtalo de nuevo en un momento.",
  "errors.provider_quota_exceeded":
    "La búsqueda no está disponible temporalmente porque alcanzamos el límite de uso del servicio de búsqueda. Inténtalo de nuevo más tarde.",
  "errors.provider_not_configured":
    "No se pudo completar la búsqueda porque el servicio de búsqueda no está disponible temporalmente. Inténtalo de nuevo en breve.",
  "errors.provider_credentials_invalid":
    "No se pudo completar la búsqueda porque el servicio de búsqueda no está disponible temporalmente. Inténtalo de nuevo en breve.",
  "errors.provider_unavailable":
    "No se pudo completar la búsqueda porque el servicio de búsqueda no está disponible temporalmente. Inténtalo de nuevo en breve.",
  "errors.search_busy":
    "Hay muchas búsquedas en curso ahora mismo. Espera un momento e inténtalo de nuevo.",
  "errors.provider_rejected_image":
    "El servicio de búsqueda rechazó esta imagen. Prueba con otra imagen o con una versión de mayor calidad.",
  "errors.provider_timeout":
    "La búsqueda tardó más de lo esperado y no pudo completarse. Comprueba tu conexión e inténtalo de nuevo.",
  "errors.client_timeout":
    "La búsqueda tardó más de lo esperado y no pudo completarse. Comprueba tu conexión e inténtalo de nuevo.",
  "errors.network_error":
    "No se pudo conectar con el servicio de búsqueda. Comprueba tu conexión e inténtalo de nuevo.",
  "errors.provider_invalid_response":
    "Algo salió mal al buscar esta imagen. Inténtalo de nuevo. Si el problema persiste, informa del código de referencia.",
  "errors.provider_error":
    "Algo salió mal al buscar esta imagen. Inténtalo de nuevo. Si el problema persiste, informa del código de referencia.",
  "errors.unexpected_error":
    "Algo salió mal al buscar esta imagen. Inténtalo de nuevo. Si el problema persiste, informa del código de referencia.",
  "errors.unknown_error":
    "Algo salió mal al buscar esta imagen. Inténtalo de nuevo. Si el problema persiste, informa del código de referencia.",
};
