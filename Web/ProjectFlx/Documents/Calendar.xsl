<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:wbt="myWebTemplater.1.0">

	<xsl:variable name="calendar" select="document('calendar.xml')/calendar"/>
	<xsl:variable name="days" select="$calendar/days/day" />
	<xsl:variable name="months" select="$calendar/months/month" />
	<xsl:variable name="calendarlayer" select="calendar/@CalendarLayer"/>

	<wbt:intervaltypes>
		<type>Days</type>
		<type>Weeks</type>
		<type>Months</type>
		<type>Years</type>
	</wbt:intervaltypes>
	
	<xsl:template match="calendar[@intervaltype='Days']">
		<xsl:apply-templates mode="interval_weeks"/>
	</xsl:template>
	
	<xsl:template match="calendar[@intervaltype='Weeks']">
		<xsl:apply-templates select="months/month" mode="interval_weeks"/>
	</xsl:template>
	
	<xsl:template match="calendar[@intervaltype='Months']">
		<xsl:apply-templates select="months" mode="interval_months"/>
	</xsl:template>
	
	<xsl:template match="calendar[@intervaltype='Years']">
		<xsl:apply-templates mode="interval_weeks"/>
	</xsl:template>

	<!-- normal display-->
	<xsl:template match="month[@value=/calendar/@month]" mode="interval_weeks">

		<table class="calendar">
			<!-- caption -->

			<caption>
				<span class="floatl">
					<a href="#" onclick="return navigateCalendarForLayer('previous','{ancestor::calendar/@current}', 'false', '{$calendarlayer}')">&lt;</a>
				</span>
				<span class="floatr">
					<a href="#" onclick="return navigateCalendarForLayer('next','{ancestor::calendar/@current}', 'false', '{$calendarlayer}')">&gt;</a>
				</span>
				<span>
					<xsl:value-of select="concat(@value, ' - ', @year)"/>
				</span>
			</caption>

			<!-- load th -->
			<tr>
				<xsl:apply-templates select="$days" mode="th_abv"/>
			</tr>
			<xsl:apply-templates select="week"/>

			<!-- footer row -->
			<tr>
				<td colspan="6">
					<span class="floatl">
						<a href="#" onclick="return navigateCalendarForLayer('current', null, 'false', '{$calendarlayer}')">Today</a>
					</span>
					<span class="floatr">
						<!--<a href="#" onclick="Effect.DropOut('Calendar'); return false;">-hide-</a>-->
						<a href="#"  onclick="Element.hide('{$calendarlayer}'); return false;">-hide-</a>
					</span>
				</td>
			</tr>
		</table>

	</xsl:template>

	<xsl:template match="day" mode="th_abv">
		<th>
			<xsl:value-of select="@abv"/>
		</th>
	</xsl:template>

	<xsl:template match="week">
		<xsl:variable name="current_week" select="@value"/>
		<xsl:variable name="current_year" select="parent::month/@year"/>
		<tr>
			<xsl:apply-templates select="/calendar/days/day[@week=$current_week][@year=$current_year]"/>
		</tr>
	</xsl:template>

	<xsl:template match="day">
		<xsl:variable name="kill" select="boolean(@month = /calendar/@month)"/>
		<xsl:variable name="class">
			<xsl:if test="not(@month = /calendar/@month)">
				<xsl:text>inactive_month </xsl:text>
			</xsl:if>
			<xsl:if test=". = ancestor::calendar/@current">
				<xsl:text>current </xsl:text>
			</xsl:if>
		</xsl:variable>
		<td>
			<xsl:attribute name="title">
				<xsl:value-of select="."/>
			</xsl:attribute>
			<xsl:attribute name="class">
				<xsl:value-of select="$class"/>
			</xsl:attribute>
			<a href="#" onclick="return navigateCalendarForLayer(null, '{.}', '{$kill}', '{$calendarlayer}')">
				<xsl:value-of select="@value"/>
			</a>
		</td>
	</xsl:template>


	<!-- months display -->
	<xsl:template match="months" mode="interval_months">
		<html>
		<ul class="calendar">
			<xsl:apply-templates select="month" mode="interval_months"/>
		</ul>
		</html>
	</xsl:template>
	<xsl:template match="month" mode="interval_months">
		<li>
			<xsl:value-of select="concat(@value, ' - ', @year)"/>
		</li>
	</xsl:template>
	
	</xsl:stylesheet> 

